namespace The_Hirelo.DTOs.Responses
{
    public class CvParseResultResponse
    {
        /// <summary>Partition Key - ID của candidate</summary>
        public string? CandidateId { get; set; }

        /// <summary>Sort Key - S3 object key của file CV đã upload</summary>
        public string? ObjectKey { get; set; }

        /// <summary>Trạng thái parse: SUCCEEDED | FAILED | PENDING</summary>
        public string? ParseStatus { get; set; }

        /// <summary>true nếu parse thành công</summary>
        public bool ProcessSuccess { get; set; }

        /// <summary>Nội dung CV sau khi parse (nested JSON từ Lambda)</summary>
        public Dictionary<string, object>? ParsedResult { get; set; }

        /// <summary>Thông báo lỗi nếu parse thất bại</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Unix timestamp (seconds) lúc xử lý xong</summary>
        public long? ProcessedAt { get; set; }

        /// <summary>ISO 8601 timestamp cập nhật gần nhất</summary>
        public string? UpdatedAt { get; set; }

        /// <summary>Profile ID (thường giống CandidateId)</summary>
        public string? ProfileId { get; set; }
    }
}
