using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly HireloDbContext _context;
        public CompanyRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<Company?> GetByIdAsync(Guid id)
        {
            return await _context.Companies.FindAsync(id);
        }

        public async Task<Company?> GetByRecruiterProfileIdAsync(Guid recruiterProfileId)
        {
            var recruiter = await _context.RecruiterProfiles.Include(r => r.Company).FirstOrDefaultAsync(r => r.Id == recruiterProfileId);
            return recruiter?.Company;
        }

        public async Task<Company> UpdateAsync(Company company)
        {
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();
            return company;
        }
    }
}
