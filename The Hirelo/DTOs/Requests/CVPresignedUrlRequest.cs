namespace The_Hirelo.DTOs.Requests
{
    public class CVPresignedUrlRequest
    {
        public string FileName { get; set; } = "";
        public string? ContentType { get; set; }
        public Guid? JobId { get; set; }
    }
}
