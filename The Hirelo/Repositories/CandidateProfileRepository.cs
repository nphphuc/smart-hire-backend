using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.Models;

namespace The_Hirelo.Repositories
{
    public class CandidateProfileRepository : ICandidateProfileRepository
    {
        private readonly HireloDbContext _context;

        public CandidateProfileRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<CandidateProfile?> GetByIdAsync(Guid profileId)
            => await _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.Job)
                    .ThenInclude(j => j!.Recruiter)
                .FirstOrDefaultAsync(p => p.Id == profileId);

        public async Task<List<CandidateProfile>> GetByJobIdAsync(Guid jobId)
            => await _context.CandidateProfiles
                .Include(p => p.User)
                .Where(p => p.JobId == jobId)
                .OrderByDescending(p => p.MatchingScore)
                .ToListAsync();

        public async Task<CandidateProfile> CreateAsync(CandidateProfile profile)
        {
            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task UpdateAsync(CandidateProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _context.CandidateProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }
    }
}
