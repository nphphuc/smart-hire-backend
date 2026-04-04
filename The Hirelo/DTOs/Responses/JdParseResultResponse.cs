namespace The_Hirelo.DTOs.Responses
{
    public class JdParseResultResponse
    {
        /// <summary>Partition Key - ID của job posting</summary>
        public string? JobId { get; set; }

        /// <summary>Tiêu đề job (Unknown nếu chưa parse xong)</summary>
        public string? JobTitle { get; set; }

        /// <summary>Nội dung văn bản JD sau khi extract từ file</summary>
        public string? JdText { get; set; }

        // Không có trong bảng Job PostgreSQL (chỉ có trên DynamoDB cũ)
        // /// <summary>S3 object key của file JD gốc đã upload</summary>
        // public string? OriginalFileKey { get; set; }

        // Không có trong bảng Job PostgreSQL (chỉ có trên DynamoDB cũ)
        // /// <summary>Trạng thái parse: SUCCEEDED | FAILED | PENDING</summary>
        // public string? ParseStatus { get; set; }

        // Không có trong bảng Job PostgreSQL (chỉ có trên DynamoDB cũ)
        // /// <summary>Danh sách kỹ năng yêu cầu được extract từ JD</summary>
        // public List<string>? RequiredSkills { get; set; }

        /// <summary>ISO 8601 timestamp cập nhật gần nhất</summary>
        public string? UpdatedAt { get; set; }
    }
}
