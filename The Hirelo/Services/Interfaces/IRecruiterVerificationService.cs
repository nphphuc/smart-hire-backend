using The_Hirelo.DTOs.Requests;

namespace The_Hirelo.Services.Interfaces
{
    public interface IRecruiterVerificationService
    {
        Task<Guid> SubmitAsync(Guid recruiterProfileId, RecruiterVerificationRequest request);
    }
}
