using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services
{
    public class AdminService : IAdminService
    {
        private readonly HireloDbContext _context;
        private readonly ILogger<AdminService> _logger;

        public AdminService(HireloDbContext context, ILogger<AdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(int total, List<AdminUserResponse> data)> GetUsersAsync(
            string? role, int page, int pageSize)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role,
                    CognitoSub = u.CognitoSub,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return (total, data);
        }

        public async Task<(int total, List<AdminCandidateResponse> data)> GetCandidatesAsync(
            string? status, Guid? jobId, int page, int pageSize)
        {
            var query = _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.Job)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (jobId.HasValue)
                query = query.Where(p => p.JobId == jobId);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminCandidateResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Email = p.User.Email,
                    JobId = p.JobId,
                    JobTitle = p.Job != null ? p.Job.Title : null,
                    Seniority = p.Seniority,
                    MatchingScore = p.MatchingScore,
                    Status = p.Status,
                    FileUrl = p.FileUrl,
                    Strengths = p.Strengths,
                    Gaps = p.Gaps,
                    ParsedSkillsJson = p.ParsedSkillsJson,
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return (total, data);
        }

        public async Task<AdminCandidateResponse?> GetCandidateByIdAsync(Guid profileId)
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null) return null;

            return new AdminCandidateResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Email = profile.User?.Email ?? "",
                JobId = profile.JobId,
                JobTitle = profile.Job?.Title,
                Seniority = profile.Seniority,
                MatchingScore = profile.MatchingScore,
                Status = profile.Status,
                FileUrl = profile.FileUrl,
                Strengths = profile.Strengths,
                Gaps = profile.Gaps,
                ParsedSkillsJson = profile.ParsedSkillsJson,
                CreatedAt = profile.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = profile.UpdatedAt
            };
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin deleted user {UserId}", userId);
            return true;
        }

        public async Task<bool> UpdateUserRoleAsync(Guid userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Role = role;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin updated role for user {UserId} to {Role}", userId, role);
            return true;
        }
    }
}