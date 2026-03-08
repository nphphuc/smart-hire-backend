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

        public RecruiterVerificationService(
        IRecruiterVerificationRepository repository,
        IFileStorage storage)
        {
            _repository = repository;
            _storage = storage;
        }

        public async Task<Guid> SubmitAsync(Guid recruiterProfileId, RecruiterVerificationRequest request)
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
                RecruiterProfileId = recruiterProfileId,
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

        private async Task<string> UploadFile(IFormFile file)
        {
            var key = $"verifications/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            return await _storage.UploadAsync(file, key);
        }
    }
}
