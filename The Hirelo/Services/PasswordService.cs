using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Security.Cryptography;
using System.Text;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;
using ChangePasswordRequestDTO = The_Hirelo.DTOs.Requests.ChangePasswordRequest;
using ForgotPasswordRequestDTO = The_Hirelo.DTOs.Requests.ForgotPasswordRequest;

namespace The_Hirelo.Services;

public class PasswordService : IPasswordService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _clientId;
    private readonly string _userPoolId;
    private readonly string? _clientSecret;

    public PasswordService(
        IAmazonCognitoIdentityProvider cognitoClient,
        IConfiguration configuration)
    {
        _cognitoClient = cognitoClient;
        _clientId = configuration["AWS:Cognito:ClientId"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientId")!;
        _userPoolId = configuration["AWS:Cognito:UserPoolId"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId")!;
        _clientSecret = configuration["AWS:Cognito:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequestDTO request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
            {
                // Don't reveal if user exists
                return (true, "If the email exists, a reset code has been sent.");
            }

            var forgotRequest = new Amazon.CognitoIdentityProvider.Model.ForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = username
            };

            await _cognitoClient.ForgotPasswordAsync(forgotRequest);

            return (true, "Password reset code sent to your email.");
        }
        catch (UserNotFoundException)
        {
            return (true, "If the email exists, a reset code has been sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordService.ForgotPasswordAsync] Error: {ex.Message}");
            return (false, $"Failed to initiate password reset: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
                return (false, "User not found.");

            var confirmRequest = new ConfirmForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = username,
                ConfirmationCode = request.ConfirmationCode,
                Password = request.NewPassword
            };

            await _cognitoClient.ConfirmForgotPasswordAsync(confirmRequest);

            return (true, "Password reset successfully. You can now login with your new password.");
        }
        catch (CodeMismatchException)
        {
            return (false, "Invalid reset code.");
        }
        catch (ExpiredCodeException)
        {
            return (false, "Reset code has expired. Please request a new one.");
        }
        catch (InvalidPasswordException ex)
        {
            return (false, $"Invalid password: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordService.ResetPasswordAsync] Error: {ex.Message}");
            return (false, $"Password reset failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordRequestDTO request, string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var changeRequest = new Amazon.CognitoIdentityProvider.Model.ChangePasswordRequest
            {
                AccessToken = accessToken,
                PreviousPassword = request.CurrentPassword,
                ProposedPassword = request.NewPassword
            };

            await _cognitoClient.ChangePasswordAsync(changeRequest);

            return (true, "Password changed successfully.");
        }
        catch (NotAuthorizedException)
        {
            return (false, "Current password is incorrect.");
        }
        catch (InvalidPasswordException ex)
        {
            return (false, $"Invalid new password: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordService.ChangePasswordAsync] Error: {ex.Message}");
            return (false, $"Password change failed: {ex.Message}");
        }
    }

    // Private helpers
    private string ComputeSecretHash(string username)
    {
        if (string.IsNullOrEmpty(_clientSecret)) return string.Empty;
        var data = Encoding.UTF8.GetBytes(username + _clientId);
        var key = Encoding.UTF8.GetBytes(_clientSecret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    private async Task<string?> FindUsernameByEmail(string email)
    {
        try
        {
            Console.WriteLine($"[PasswordService.FindUsernameByEmail] Searching for email: {email}");

            var listRequest = new ListUsersRequest
            {
                UserPoolId = _userPoolId,
                Filter = $"email = \"{email}\"",
                Limit = 1
            };

            var response = await _cognitoClient.ListUsersAsync(listRequest);
            var user = response.Users?.FirstOrDefault();

            if (user != null)
            {
                Console.WriteLine($"[PasswordService.FindUsernameByEmail] Found user via ListUsers: {user.Username}");
                return user.Username;
            }

            // If not found by ListUsers, try using email as username directly
            try
            {
                var adminGetRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };
                var adminResponse = await _cognitoClient.AdminGetUserAsync(adminGetRequest);
                Console.WriteLine($"[PasswordService.FindUsernameByEmail] Found user via AdminGetUser: {adminResponse.Username}");
                return adminResponse.Username;
            }
            catch (UserNotFoundException)
            {
                Console.WriteLine($"[PasswordService.FindUsernameByEmail] User not found with email as username: {email}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordService.FindUsernameByEmail] Error for {email}: {ex.Message}");
            return null;
        }
    }
}
