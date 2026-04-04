using The_Hirelo.Data;
using The_Hirelo.Enums;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly HireloDbContext _context;

        public ApplicationRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<Application?> CreateApplicationAsync(Guid jobId, Guid candidateId)
        {
            // Check if application already exists
            var existing = await _context.Applications
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.CandidateId == candidateId);

            if (existing != null)
            {
                return existing;
            }

            var application = new Application
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                CandidateId = candidateId,
                Status = ApplicationStatus.Applied,
                AppliedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return application;
        }

        public async Task<Application?> GetApplicationByIdAsync(Guid applicationId)
        {
            return await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
        }

        public async Task<IEnumerable<Application>> GetApplicationsByJobIdAsync(Guid jobId)
        {
            return await _context.Applications
                .Where(a => a.JobId == jobId)
                .Include(a => a.Candidate)
                    .ThenInclude(c => c.User)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetApplicationsByCandidateIdAsync(Guid candidateId)
        {
            return await _context.Applications
                .Where(a => a.CandidateId == candidateId)
                .Include(a => a.Job)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
        }

        public async Task<Application?> GetApplicationAsync(Guid jobId, Guid candidateId)
        {
            return await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.Candidate)
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.CandidateId == candidateId);
        }

        public async Task<bool> ApplicationExistsAsync(Guid jobId, Guid candidateId)
        {
            return await _context.Applications
                .AnyAsync(a => a.JobId == jobId && a.CandidateId == candidateId);
        }

        public async Task<bool> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus status)
        {
            var application = await _context.Applications.FindAsync(applicationId);
            if (application == null)
                return false;

            application.Status = status;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteApplicationAsync(Guid applicationId)
        {
            var application = await _context.Applications.FindAsync(applicationId);
            if (application == null)
                return false;

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
