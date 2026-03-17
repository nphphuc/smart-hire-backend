using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace The_Hirelo.Storage
{
    public class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;
        private readonly string _awsRegion;
        private readonly ILogger<S3FileStorage> _logger;

        public S3FileStorage(IAmazonS3 s3, IConfiguration configuration, ILogger<S3FileStorage> logger)
        {
            _s3 = s3;
            _bucketName = configuration["AWS:S3:BucketName"] ?? "hirelo-media";
            _awsRegion = configuration["AWS:Region"] ?? "ap-southeast-1";
            _logger = logger;

            _logger.LogInformation($"S3FileStorage initialized with bucket: {_bucketName}, region: {_awsRegion}");
        }

        public async Task<string> UploadAsync(IFormFile file, string key)
        {
            try
            {
                _logger.LogInformation($"Attempting to upload file to S3: {key}");

                using var stream = file.OpenReadStream();

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                var response = await _s3.PutObjectAsync(request);
                
                _logger.LogInformation($"Successfully uploaded file to S3: {key}");

                // Generate proper S3 URL with region
                var url = $"https://{_bucketName}.s3.{_awsRegion}.amazonaws.com/{key}";
                _logger.LogInformation($"Generated S3 URL: {url}");
                
                return url;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError($"S3 Error uploading {key}: {ex.ErrorCode} - {ex.Message}");
                
                if (ex.ErrorCode == "InvalidAccessKeyId")
                {
                    throw new Exception("AWS Access Key ID is invalid. Check .env file.", ex);
                }
                else if (ex.ErrorCode == "SignatureDoesNotMatch")
                {
                    throw new Exception("AWS Secret Access Key is invalid. Check .env file.", ex);
                }
                else if (ex.ErrorCode == "NoSuchBucket")
                {
                    throw new Exception($"S3 bucket '{_bucketName}' does not exist. Check appsettings.json.", ex);
                }
                else if (ex.ErrorCode == "AccessDenied")
                {
                    throw new Exception($"Access Denied to S3 bucket '{_bucketName}'. Check IAM permissions.", ex);
                }
                
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error uploading to S3: {ex.Message}");
                throw;
            }
        }
    }
}
