using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Repositories.Interfaces;

namespace The_Hirelo.Repositories
{
    public class CvRepository : ICvRepository
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;

        public CvRepository(IAmazonDynamoDB dynamoDb, IConfiguration configuration)
        {
            _dynamoDb = dynamoDb;
            // Đọc tên bảng từ appsettings/env, mặc định là tên table prod
            _tableName = Environment.GetEnvironmentVariable("DYNAMODB_CV_TABLE")
                ?? configuration["AWS:DynamoDB:CvTableName"]
                ?? "smarthire-cv-parse-results-dev";
        }

        /// <inheritdoc />
        public async Task<CvParseResultResponse?> GetLatestCvByCandidateIdAsync(string candidateId)
        {
            var all = await GetAllCvsByCandidateIdAsync(candidateId);
            // Sắp xếp theo ProcessedAt giảm dần, lấy bản ghi mới nhất
            return all
                .OrderByDescending(cv => cv.ProcessedAt ?? 0)
                .FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CvParseResultResponse>> GetAllCvsByCandidateIdAsync(string candidateId)
        {
            var request = new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "candidateId = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = candidateId } }
                }
            };

            var response = await _dynamoDb.QueryAsync(request);
            return response.Items.Select(MapToResponse).ToList();
        }

        // -------------------------------------------------------
        // Private mapper: DynamoDB AttributeValue -> DTO
        // -------------------------------------------------------
        private static CvParseResultResponse MapToResponse(Dictionary<string, AttributeValue> item)
        {
            return new CvParseResultResponse
            {
                CandidateId   = GetString(item, "candidateId"),
                ObjectKey     = GetString(item, "objectKey"),
                ParseStatus   = GetString(item, "parseStatus"),
                ProcessSuccess = GetBool(item, "ProcessSuccess"),
                ErrorMessage  = GetString(item, "errorMessage"),
                ProcessedAt   = GetLong(item, "ProcessedAt"),
                UpdatedAt     = GetString(item, "updatedAt"),
                ProfileId     = GetString(item, "ProfileId"),
                ParsedResult  = GetMap(item, "parsedResult")
            };
        }

        // ---- helpers ----
        private static string? GetString(Dictionary<string, AttributeValue> item, string key)
            => item.TryGetValue(key, out var attr) ? attr.S : null;

        private static bool GetBool(Dictionary<string, AttributeValue> item, string key)
            => item.TryGetValue(key, out var attr) && (attr.BOOL == true);

        private static long? GetLong(Dictionary<string, AttributeValue> item, string key)
        {
            if (item.TryGetValue(key, out var attr) && long.TryParse(attr.N, out var val))
                return val;
            return null;
        }

        /// <summary>Chuyển DynamoDB Map (AttributeValue) sang Dictionary&lt;string, object&gt;</summary>
        private static Dictionary<string, object>? GetMap(Dictionary<string, AttributeValue> item, string key)
        {
            if (!item.TryGetValue(key, out var attr) || attr.M == null)
                return null;

            return ConvertAttributeMap(attr.M);
        }

        private static Dictionary<string, object> ConvertAttributeMap(Dictionary<string, AttributeValue> map)
        {
            var result = new Dictionary<string, object>();
            foreach (var (k, v) in map)
            {
                result[k] = ConvertAttributeValue(v);
            }
            return result;
        }

        private static object ConvertAttributeValue(AttributeValue attr)
        {
            if (attr.S != null)        return attr.S;
            if (attr.N != null)        return attr.N;  // giữ string để tránh precision loss
            if (attr.BOOL == true)     return true;
            if (attr.BOOL == false && attr.NULL != true) return false;
            if (attr.NULL == true)     return null!;
            if (attr.M != null)        return ConvertAttributeMap(attr.M);
            if (attr.L != null)        return attr.L.Select(ConvertAttributeValue).ToList();
            if (attr.SS != null)       return attr.SS;
            if (attr.NS != null)       return attr.NS;
            return string.Empty;
        }
    }
}
