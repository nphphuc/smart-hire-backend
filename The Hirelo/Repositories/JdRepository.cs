// ============================================================
// JdRepository - đã chuyển từ DynamoDB sang PostgreSQL (Job table)
// Code DynamoDB cũ được comment lại, không xóa.
// ============================================================

// using Amazon.DynamoDBv2;
// using Amazon.DynamoDBv2.Model;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Repositories.Interfaces;

namespace The_Hirelo.Repositories
{
    public class JdRepository : IJdRepository
    {
        // ---- PostgreSQL (mới) ----
        private readonly HireloDbContext _context;

        public JdRepository(HireloDbContext context)
        {
            _context = context;
        }

        // ---- DynamoDB (cũ - đã comment) ----
        // private readonly IAmazonDynamoDB _dynamoDb;
        // private readonly string _tableName;
        //
        // public JdRepository(IAmazonDynamoDB dynamoDb, IConfiguration configuration)
        // {
        //     _dynamoDb = dynamoDb;
        //     _tableName = Environment.GetEnvironmentVariable("DYNAMODB_JD_TABLE")
        //         ?? configuration["AWS:DynamoDB:JdTableName"]
        //         ?? "smarthire-jd-parse-results-dev";
        // }

        /// <inheritdoc />
        public async Task<JdParseResultResponse?> GetJdByJobIdAsync(string jobId)
        {
            if (!Guid.TryParse(jobId, out var id)) return null;

            var job = await _context.Jobs
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return null;
            return MapToResponse(job);

            // ---- DynamoDB (cũ) ----
            // var request = new GetItemRequest
            // {
            //     TableName = _tableName,
            //     Key = new Dictionary<string, AttributeValue>
            //     {
            //         { "jobId", new AttributeValue { S = jobId } }
            //     }
            // };
            // var response = await _dynamoDb.GetItemAsync(request);
            // if (response.Item == null || response.Item.Count == 0)
            //     return null;
            // return MapToResponse(response.Item);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<JdParseResultResponse>> GetAllJdsAsync()
        {
            var jobs = await _context.Jobs
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.User)
                .ToListAsync();

            return jobs.Select(MapToResponse);

            // ---- DynamoDB (cũ) ----
            // var results = new List<JdParseResultResponse>();
            // Dictionary<string, AttributeValue>? lastKey = null;
            // do
            // {
            //     var request = new ScanRequest
            //     {
            //         TableName = _tableName,
            //         ExclusiveStartKey = lastKey
            //     };
            //     var response = await _dynamoDb.ScanAsync(request);
            //     results.AddRange(response.Items.Select(MapToResponse));
            //     lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
            // }
            // while (lastKey != null);
            // return results;
        }

        // -------------------------------------------------------
        // Private mapper: Job (PostgreSQL) -> JdParseResultResponse
        // -------------------------------------------------------
        private static JdParseResultResponse MapToResponse(Models.Job job)
        {
            return new JdParseResultResponse
            {
                JobId    = job.Id.ToString(),
                JobTitle = job.Title,
                JdText   = job.Description,
                // OriginalFileKey không có trong bảng Job (bảng DynamoDB cũ) - đang dùng JdFileUrl thay thế
                // OriginalFileKey = null,
                // ParseStatus không có trong bảng Job (chỉ có trên DynamoDB)
                // ParseStatus = null,
                // RequiredSkills không có trong bảng Job (chỉ có trên DynamoDB)
                // RequiredSkills = null,
                UpdatedAt = job.CreatedAt?.ToString("o")
            };
        }

        // ---- DynamoDB mapper (cũ - đã comment) ----
        // private static JdParseResultResponse MapToResponse(Dictionary<string, AttributeValue> item)
        // {
        //     return new JdParseResultResponse
        //     {
        //         JobId           = GetString(item, "jobId"),
        //         JobTitle        = GetString(item, "jobTitle"),
        //         JdText          = GetString(item, "jdText"),
        //         OriginalFileKey = GetString(item, "originalFileKey"),
        //         ParseStatus     = GetString(item, "parseStatus"),
        //         RequiredSkills  = GetStringList(item, "requiredSkills"),
        //         UpdatedAt       = GetString(item, "updatedAt")
        //     };
        // }

        // ---- helpers (DynamoDB - cũ) ----
        // private static string? GetString(Dictionary<string, AttributeValue> item, string key)
        //     => item.TryGetValue(key, out var attr) ? attr.S : null;
        //
        // private static List<string>? GetStringList(Dictionary<string, AttributeValue> item, string key)
        // {
        //     if (!item.TryGetValue(key, out var attr))
        //         return null;
        //     if (attr.L != null && attr.L.Count > 0)
        //         return attr.L.Select(a => a.S ?? string.Empty).ToList();
        //     if (attr.SS != null && attr.SS.Count > 0)
        //         return attr.SS;
        //     return null;
        // }
    }
}
