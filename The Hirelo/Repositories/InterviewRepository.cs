using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class InterviewRepository : IInterviewRepository
    {
        private readonly HireloDbContext _context;
        public InterviewRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<InterviewSession?> GetByCandidateIdAsync(Guid candidateId)
        {
            return await _context.InterviewSessions
                .Include(s => s.Scorecard)
                .Include(s => s.InterviewReport)
                .Include(s => s.Candidate).ThenInclude(c => c.User)
                .Include(s => s.Job)
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<InterviewSession?> GetByIdAsync(Guid id)
        {
            return await _context.InterviewSessions
                .Include(s => s.Scorecard)
                .Include(s => s.InterviewReport)
                .Include(s => s.Candidate).ThenInclude(c => c.User)
                .Include(s => s.Job)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<InterviewSession> GetMediaAsync(Guid interviewId)
        {
            var session = await _context.InterviewSessions
                .Include(s => s.EmotionFrames)
                .Include(s => s.CodeSubmissions)
                .FirstOrDefaultAsync(s => s.Id == interviewId);
            return session!;
        }
    }
}
