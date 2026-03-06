using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Models;

namespace The_Hirelo.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        public CompanyService(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public async Task<Company?> GetByRecruiterProfileIdAsync(Guid recruiterProfileId)
        {
            return await _companyRepository.GetByRecruiterProfileIdAsync(recruiterProfileId);
        }

        public async Task<Company?> GetByIdAsync(Guid id)
        {
            return await _companyRepository.GetByIdAsync(id);
        }

        public async Task<Company> UpdateAsync(Company company)
        {
            return await _companyRepository.UpdateAsync(company);
        }
    }
}
