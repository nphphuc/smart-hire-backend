using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Services;

public class UserService : IUserService
{
    private readonly HireloDbContext _context;

    public UserService(HireloDbContext context)
    {
        _context = context;
    }

    public async Task<UserInfoResponse?> GetCurrentUserAsync(string cognitoSub)
    {
        try
        {
            if (string.IsNullOrEmpty(cognitoSub))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);

            if (user == null)
                return null;

            return new UserInfoResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService.GetCurrentUserAsync] Error: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateUserRoleAsync(Guid userId, UserRole role, string? adminCognitoSub = null)
    {
        try
        {
            // Verify admin permission if adminCognitoSub is provided
            if (!string.IsNullOrEmpty(adminCognitoSub))
            {
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.CognitoSub == adminCognitoSub);

                if (adminUser == null || adminUser.Role != UserRole.Admin)
                {
                    return (false, "Only admins can update user roles.");
                }
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "User not found.");

            var validRoles = new[] { UserRole.Admin, UserRole.Recruiter, UserRole.Candidate };
            if (!validRoles.Contains(role))
                return (false, "Invalid role. Valid roles: Admin, Recruiter, Candidate");

            user.Role = role;
            await _context.SaveChangesAsync();

            return (true, "Role updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService.UpdateUserRoleAsync] Error: {ex.Message}");
            return (false, $"Failed to update user role: {ex.Message}");
        }
    }

    public async Task<Guid> SubmitRecruiterVerificationAsync(string cognitoSub, RecruiterVerificationRequest request, IRecruiterVerificationService recruiterVerificationService)
    {
        try
        {
            if (string.IsNullOrEmpty(cognitoSub))
                throw new UnauthorizedAccessException("Invalid token.");

            // Get user from database
            var user = await _context.Users
                .Include(u => u.RecruiterProfile)
                .FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);

            if (user?.RecruiterProfile == null)
                throw new InvalidOperationException("User does not have a recruiter profile.");

            var recruiterProfileId = user.RecruiterProfile.Id;
            var id = await recruiterVerificationService.SubmitAsync(recruiterProfileId, request);

            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService.SubmitRecruiterVerificationAsync] Error: {ex.Message}");
            throw;
        }
    }
}
