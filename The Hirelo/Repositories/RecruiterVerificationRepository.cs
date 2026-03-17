using System;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;

namespace The_Hirelo.Repositories
{
    public class RecruiterVerificationRepository : IRecruiterVerificationRepository
    {
        private readonly HireloDbContext _db;

        public RecruiterVerificationRepository(HireloDbContext db)
        {
            _db = db;
        }
        
        public async Task AddAsync(RecruiterVerification verification)
        {
            _db.RecruiterVerifications.Add(verification);
            await _db.SaveChangesAsync();
        }

        public async Task<RecruiterVerification?> GetByIdAsync(Guid verificationId)
        {
            return await _db.RecruiterVerifications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == verificationId);
        }

        public async Task<List<RecruiterVerification>> GetAllAsync()
        {
            return await _db.RecruiterVerifications
                .AsNoTracking()
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<RecruiterVerification?> GetByUserIdAsync(Guid userId)
        {
            return await _db.RecruiterVerifications
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId);
        }

        public async Task UpdateAsync(RecruiterVerification verification)
        {
            _db.RecruiterVerifications.Update(verification);
            await _db.SaveChangesAsync();
        }
    }
}
