using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly HireloDbContext _context;
        public JobRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<Job> CreateAsync(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task DeleteAsync(Guid id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Job>> GetByRecruiterIdAsync(Guid recruiterId)
        {
            return await _context.Jobs
                .Where(j => j.RecruiterId == recruiterId)
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.User)
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.Company)
                .ToListAsync();
        }

        public async Task<Job?> GetByIdAsync(Guid id)
        {
            return await _context.Jobs
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.User)
                .Include(j => j.Recruiter)
                    .ThenInclude(r => r.Company)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<Job> UpdateAsync(Job job)
        {
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task SaveJdFileMetadataAsync(Guid jobId, string fileUrl)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return;
            job.JdFileUrl = fileUrl;
            await _context.SaveChangesAsync();
        }
    }
}
