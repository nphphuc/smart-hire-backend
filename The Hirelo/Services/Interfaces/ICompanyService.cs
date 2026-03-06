using The_Hirelo.Models;

namespace The_Hirelo.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<Company?> GetByRecruiterProfileIdAsync(Guid recruiterProfileId);
        Task<Company?> GetByIdAsync(Guid id);
        Task<Company> UpdateAsync(Company company);
    }
}
