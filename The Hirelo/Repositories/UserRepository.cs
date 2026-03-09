using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly HireloDbContext _context;
        public UserRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(The_Hirelo.Enums.UserRole role)
        {
            return await _context.Users.Where(u => u.Role == role).ToListAsync();
        }

        public async Task<User?> GetByCognitoSubAsync(string sub)
        {
            return await _context.Users.Include(u => u.RecruiterProfile).ThenInclude(r => r.Company)
                .Include(u => u.CandidateProfile)
                .FirstOrDefaultAsync(u => u.CognitoSub == sub);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.Include(u => u.RecruiterProfile).ThenInclude(r => r.Company)
                .Include(u => u.CandidateProfile)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> UpdateAsync(Guid id, User user)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return user;
            existing.Email = user.Email ?? existing.Email;
            existing.Role = user.Role;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
