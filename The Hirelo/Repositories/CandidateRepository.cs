using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Enums;

namespace The_Hirelo.Repositories
{
    public class CandidateRepository : ICandidateRepository
    {
        private readonly HireloDbContext _context;
        public CandidateRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(Guid jobId, Guid candidateId)
        {
            return await _context.InterviewSessions.AnyAsync(s => s.JobId == jobId && s.CandidateId == candidateId);
        }

        public async Task<IEnumerable<User>> GetCandidatesByJobIdAsync(Guid jobId)
        {
            var sessions = await _context.InterviewSessions
                .Where(s => s.JobId == jobId)
                .Include(s => s.Candidate)
                    .ThenInclude(c => c.User)
                .ToListAsync();

            return sessions.Select(s => s.Candidate.User).Distinct();
        }

        // Get latest status for candidate across all jobs
        public async Task<ApplicationStatus?> GetStatusAsync(Guid candidateId)
        {
            var session = await _context.InterviewSessions
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
            return session?.Status;
        }

        // Update latest session status for candidate
        public async Task UpdateStatusAsync(Guid candidateId, ApplicationStatus status)
        {
            var session = await _context.InterviewSessions
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
            if (session == null) return;
            session.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByCandidateProfileIdAsync(Guid candidateProfileId)
        {
            var candidate = await _context.CandidateProfiles
                .Include(cp => cp.User)
                .FirstOrDefaultAsync(cp => cp.Id == candidateProfileId);
            return candidate?.User;
        }
    }
}
