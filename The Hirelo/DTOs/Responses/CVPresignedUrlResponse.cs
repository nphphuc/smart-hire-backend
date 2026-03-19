namespace The_Hirelo.DTOs.Responses
{
    public class CVPresignedUrlResponse
    {
        public string PresignedUrl { get; set; } = "";
        public string FileKey { get; set; } = "";
        public Guid ProfileId { get; set; }
        public int ExpiresInSeconds { get; set; }
    }
}
