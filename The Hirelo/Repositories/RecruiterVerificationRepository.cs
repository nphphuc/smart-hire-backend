using System;
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
            await _db.SaveChangesAsync(); ;
        }
    }
}
