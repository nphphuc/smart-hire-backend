using The_Hirelo.Services.Interfaces;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services
{
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        public CandidateService(ICandidateRepository candidateRepository)
        {
            _candidateRepository = candidateRepository;
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
    }
}
