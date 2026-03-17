using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services.Interfaces
{
    public interface IReportService
    {
        Task<ScorecardResponse> GetCandidateScorecardAsync(Guid candidateId);
        Task<ReportSummaryResponse> GenerateReportAsync(Guid interviewId);
        Task<string> GetHtmlReportAsync(Guid reportId);
        Task<string> GetJsonReportAsync(Guid reportId);
    }
}
