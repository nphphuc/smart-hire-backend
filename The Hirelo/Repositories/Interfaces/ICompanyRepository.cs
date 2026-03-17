using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByIdAsync(Guid id);
        Task<Company?> GetByRecruiterProfileIdAsync(Guid recruiterProfileId);
        Task<Company> UpdateAsync(Company company);
    }
}
