using Microsoft.AspNetCore.Routing.Matching;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services.Interfaces
{
    public interface ICandidateService
    {
        Task<IEnumerable<CandidateListItemResponse>> GetCandidatesByJobAsync(Guid jobId);
        Task<CandidateDetailResponse> GetCandidateDetailsAsync(Guid candidateId);
        Task UpdateCandidateStatusAsync(Guid candidateId, ApplicationStatus status);

        /// <summary>Lấy CV mới nhất của candidate (dùng cho Candidate tự xem CV của mình)</summary>
        Task<CvParseResultResponse?> GetLatestCvByCandidateIdAsync(string candidateId);

        /// <summary>Lấy tất cả CV của candidate (dùng cho Recruiter/Admin)</summary>
        Task<IEnumerable<CvParseResultResponse>> GetAllCvsByCandidateIdAsync(string candidateId);
        /// <summary>Kiểm tra candidate đã apply vào ít nhất 1 job của recruiter này chưa</summary>
        Task<bool> HasCandidateAppliedToRecruiterJobAsync(Guid candidateId, Guid recruiterProfileId);
    }
}
