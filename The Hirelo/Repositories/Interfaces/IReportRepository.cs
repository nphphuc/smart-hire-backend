using System.Composition;
using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IReportRepository
    {
        Task<InterviewReport?> GetByInterviewIdAsync(Guid interviewId);
        Task<InterviewReport> SaveAsync(InterviewReport report);
        Task<InterviewReport?> GetByIdAsync(Guid id);
        Task<string> GetHtmlContentAsync(Guid reportId); // Lấy nội dung HTML từ file
        Task<string> GetJsonContentAsync(Guid reportId); // Lấy nội dung JSON từ file
    }
}
