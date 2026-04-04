using The_Hirelo.Services.Interfaces;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services
{
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly ICvRepository _cvRepository;

        public CandidateService(ICandidateRepository candidateRepository, ICvRepository cvRepository)
        {
            _candidateRepository = candidateRepository;
            _cvRepository = cvRepository;
        }

        public async Task<IEnumerable<CandidateListItemResponse>> GetCandidatesByJobAsync(Guid jobId)
        {
            var users = await _candidateRepository.GetCandidatesByJobIdAsync(jobId);
            return users.Select(u => new CandidateListItemResponse
            {
                CandidateId = u.CandidateProfile!.Id,
                Email = u.Email,
                Seniority = u.CandidateProfile?.Seniority
            });
        }

        public async Task<CandidateDetailResponse> GetCandidateDetailsAsync(Guid candidateId)
        {
            var user = await _candidateRepository.GetUserByCandidateProfileIdAsync(candidateId);
            if (user == null) return null!;
            return new CandidateDetailResponse
            {
                CandidateId = user.CandidateProfile!.Id,
                Email = user.Email,
                Seniority = user.CandidateProfile?.Seniority,
                FullName = user.Email,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateCandidateStatusAsync(Guid candidateId, ApplicationStatus status)
        {
            await _candidateRepository.UpdateStatusAsync(candidateId, status);
        }

        public async Task<CvParseResultResponse?> GetLatestCvByCandidateIdAsync(string candidateId)
        {
            return await _cvRepository.GetLatestCvByCandidateIdAsync(candidateId);
        }

        public async Task<IEnumerable<CvParseResultResponse>> GetAllCvsByCandidateIdAsync(string candidateId)
        {
            return await _cvRepository.GetAllCvsByCandidateIdAsync(candidateId);
        }

        public async Task<bool> HasCandidateAppliedToRecruiterJobAsync(Guid candidateId, Guid recruiterProfileId)
        {
            return await _candidateRepository.HasCandidateAppliedToRecruiterJobAsync(candidateId, recruiterProfileId);
        }
    }
}
