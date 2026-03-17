using System.Text.Json;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Enums;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Storage;

namespace The_Hirelo.Services
{
    public class RecruiterVerificationService : IRecruiterVerificationService
    {
        private readonly IRecruiterVerificationRepository _repository;
        private readonly IFileStorage _storage;
        private readonly IUserRepository _userRepository;

        public RecruiterVerificationService(
        IRecruiterVerificationRepository repository,
        IFileStorage storage,
        IUserRepository userRepository)
        {
            _repository = repository;
            _storage = storage;
            _userRepository = userRepository;
        }

        public async Task<Guid> SubmitAsync(Guid userId, RecruiterVerificationRequest request)
        {
            var frontUrl = await UploadFile(request.CccdFront);
            var backUrl = await UploadFile(request.CccdBack);
            var cardUrl = await UploadFile(request.EmployeeCard);
            var faceUrl = await UploadFile(request.FacePhoto);

            var images = new
            {
                cccdFront = frontUrl,
                cccdBack = backUrl,
                employeeCard = cardUrl,
                facePhoto = faceUrl
            };

            var verification = new RecruiterVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyName = request.CompanyName,
                CompanyTaxCode = request.CompanyTaxCode,
                RecruiterEmail = request.RecruiterEmail,
                ImagesJson = JsonSerializer.Serialize(images),
                Status = VerificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(verification);

            return verification.Id;
        }

        public async Task<RecruiterVerification?> GetByIdAsync(Guid verificationId)
        {
            return await _repository.GetByIdAsync(verificationId);
        }

        public async Task<List<RecruiterVerification>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<RecruiterVerification?> GetByUserIdAsync(Guid userId)
        {
            return await _repository.GetByUserIdAsync(userId);
        }

        public async Task<bool> ApproveVerificationAsync(Guid userId)
        {
            var verification = await _repository.GetByUserIdAsync(userId);
            if (verification == null) return false;

            verification.Status = VerificationStatus.Approved;
            verification.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(verification);

            // Create RecruiterProfile for this user
            var user = await _userRepository.GetByIdAsync(verification.UserId);
            if (user != null && user.RecruiterProfile == null)
            {
                // Create default company if not exists
                var company = new Company
                {
                    Id = Guid.NewGuid(),
                    Name = verification.CompanyName,
                    TaxCode = verification.CompanyTaxCode,
                    CreatedAt = DateTime.UtcNow
                };

                var recruiterProfile = new RecruiterProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CompanyId = company.Id,
                    User = user,
                    Company = company,
                    IsVerified = true
                };

                user.RecruiterProfile = recruiterProfile;
                user.Role = UserRole.Recruiter;
                await _userRepository.UpdateAsync(user);
            }

            return true;
        }

        public async Task<bool> RejectVerificationAsync(Guid userId)
        {
            var verification = await _repository.GetByUserIdAsync(userId);
            if (verification == null) return false;

            verification.Status = VerificationStatus.Rejected;
            verification.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(verification);
            return true;
        }

        public async Task<bool> RemoveRecruiterRoleAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.Role = UserRole.Candidate;
            user.RecruiterProfile = null;
            await _userRepository.UpdateAsync(user);

            // Update verification status
            var verification = await _repository.GetByUserIdAsync(userId);
            if (verification != null)
            {
                verification.Status = VerificationStatus.Rejected;
                verification.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(verification);
            }

            return true;
        }

        private async Task<string> UploadFile(IFormFile file)
        {
            var key = $"verifications/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            return await _storage.UploadAsync(file, key);
        }
    }
}
