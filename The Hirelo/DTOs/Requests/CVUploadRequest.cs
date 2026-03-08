using System.ComponentModel.DataAnnotations;

namespace The_Hirelo.DTOs.Requests
{
    public class CVUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public Guid JobId { get; set; }
    }
}