using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace The_Hirelo.Lambda
{
    /// <summary>
    /// Lambda 1 — IngestionTrigger
    /// Trigger: SQS (cv-upload-queue), batch size = 10
    /// 
    /// Flow theo diagram:
    ///   SQS → Lambda1 → Validate file (size/type) → StartExecution (Step Functions)
    /// </summary>
    public class IngestionTriggerFunction
    {
        private readonly IAmazonS3 _s3;
        private readonly IAmazonStepFunctions _stepFunctions;
        private readonly ILogger<IngestionTriggerFunction> _logger;

        // Giới hạn file — đồng bộ với CVController
        private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx"];
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        private readonly string _bucketName;
        private readonly string _stateMachineArn;

        public IngestionTriggerFunction()
        {
            _s3 = new AmazonS3Client();
            _stepFunctions = new AmazonStepFunctionsClient();

            // Lấy từ Lambda Environment Variables (set trên AWS Console)
            _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME")
                ?? throw new InvalidOperationException("S3_BUCKET_NAME is not set");
            _stateMachineArn = Environment.GetEnvironmentVariable("STATE_MACHINE_ARN")
                ?? throw new InvalidOperationException("STATE_MACHINE_ARN is not set");

            // Logger đơn giản cho Lambda context
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            _logger = loggerFactory.CreateLogger<IngestionTriggerFunction>();
        }

        // Constructor dùng cho DI/Testing
        public IngestionTriggerFunction(
            IAmazonS3 s3,
            IAmazonStepFunctions stepFunctions,
            string bucketName,
            string stateMachineArn,
            ILogger<IngestionTriggerFunction> logger)
        {
            _s3 = s3;
            _stepFunctions = stepFunctions;
            _bucketName = bucketName;
            _stateMachineArn = stateMachineArn;
            _logger = logger;
        }

        /// <summary>
        /// Handler chính — SQS trigger gọi vào đây
        /// Message body từ CVService.PublishCVParseQueueAsync:
        ///   { profileId, fileKey, jobId, jdText }
        /// </summary>
        public async Task<SQSBatchResponse> HandleAsync(SQSEvent sqsEvent, ILambdaContext context)
        {
            _logger.LogInformation(
                "IngestionTrigger received {Count} messages", sqsEvent.Records.Count);

            // SQSBatchResponse để report lại message nào fail
            // → SQS sẽ retry riêng message đó, không retry cả batch
            var batchResponse = new SQSBatchResponse
            {
                BatchItemFailures = new List<SQSBatchResponse.BatchItemFailure>()
            };

            // Xử lý song song toàn bộ batch
            var tasks = sqsEvent.Records.Select(record =>
                ProcessSingleMessageAsync(record, batchResponse));

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Batch done. Success={S} | Failed={F}",
                sqsEvent.Records.Count - batchResponse.BatchItemFailures.Count,
                batchResponse.BatchItemFailures.Count);

            return batchResponse;
        }

        // ────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ────────────────────────────────────────────────────────────────

        private async Task ProcessSingleMessageAsync(
            SQSEvent.SQSMessage record,
            SQSBatchResponse batchResponse)
        {
            try
            {
                // Parse message body
                var payload = JsonSerializer.Deserialize<CVQueueMessage>(record.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload == null || string.IsNullOrEmpty(payload.FileKey))
                {
                    _logger.LogWarning("Invalid message body, skipping | MessageId={Id}", record.MessageId);
                    return; // Bỏ qua message rác — không retry
                }

                _logger.LogInformation(
                    "Processing | ProfileId={ProfileId} | FileKey={FileKey}",
                    payload.ProfileId, payload.FileKey);

                // ── BƯỚC 1: Validate file trên S3 ────────────────────────
                var isValid = await ValidateFileAsync(payload.FileKey);
                if (!isValid)
                {
                    _logger.LogWarning(
                        "File validation failed, skipping | FileKey={FileKey}", payload.FileKey);
                    return; // Không retry — file thực sự không hợp lệ
                }

                // ── BƯỚC 2: Start Step Functions ─────────────────────────
                await StartStepFunctionsAsync(payload);

                _logger.LogInformation(
                    "Step Functions started | ProfileId={ProfileId}", payload.ProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message | MessageId={Id}", record.MessageId);

                // Report failure → SQS sẽ retry message này
                lock (batchResponse.BatchItemFailures)
                {
                    batchResponse.BatchItemFailures.Add(new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier = record.MessageId
                    });
                }
            }
        }

        /// <summary>
        /// Validate file trên S3:
        ///   1. Extension có trong AllowedExtensions không?
        ///   2. File size có vượt MaxFileSizeBytes không?
        /// </summary>
        private async Task<bool> ValidateFileAsync(string fileKey)
        {
            // 1. Kiểm tra extension từ fileKey (không cần call S3)
            var ext = Path.GetExtension(fileKey).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                _logger.LogWarning("Rejected extension '{Ext}' | FileKey={Key}", ext, fileKey);
                return false;
            }

            // 2. Lấy metadata từ S3 để check file size
            try
            {
                var metadata = await _s3.GetObjectMetadataAsync(_bucketName, fileKey);

                if (metadata.ContentLength > MaxFileSizeBytes)
                {
                    _logger.LogWarning(
                        "File too large: {Size} bytes (max {Max}) | FileKey={Key}",
                        metadata.ContentLength, MaxFileSizeBytes, fileKey);
                    return false;
                }

                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found on S3 | FileKey={Key}", fileKey);
                return false;
            }
        }

        /// <summary>
        /// Bắt đầu Step Functions execution, truyền toàn bộ payload vào
        /// Step Functions sẽ tiếp tục: Textract → Comprehend → Bedrock → VectorOps
        /// </summary>
        private async Task StartStepFunctionsAsync(CVQueueMessage payload)
        {
            var input = JsonSerializer.Serialize(new
            {
                profileId = payload.ProfileId,
                fileKey = payload.FileKey,
                jobId = payload.JobId,
                jdText = payload.JdText,
                bucketName = _bucketName
            });

            // Tên execution phải unique — dùng profileId + timestamp
            var executionName = $"cv-{payload.ProfileId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            await _stepFunctions.StartExecutionAsync(new StartExecutionRequest
            {
                StateMachineArn = _stateMachineArn,
                Name = executionName,
                Input = input
            });
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // DTO — khớp với CVService.PublishCVParseQueueAsync
    // ────────────────────────────────────────────────────────────────────
    public class CVQueueMessage
    {
        public string ProfileId { get; set; } = string.Empty;
        public string FileKey { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string JdText { get; set; } = string.Empty;
    }
}
