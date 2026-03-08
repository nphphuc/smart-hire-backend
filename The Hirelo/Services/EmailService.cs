using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Security.Cryptography;
using System.Text;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Services;

public class EmailService : IEmailService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _clientId;
    private readonly string _userPoolId;
    private readonly string? _clientSecret;

    public EmailService(
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

    public async Task<(bool Success, string Message)> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
            {
                Console.WriteLine($"[EmailService.ConfirmEmailAsync] User not found for email: {request.Email}");
                return (false, "User not found.");
            }

            var confirmRequest = new ConfirmSignUpRequest
            {
                ClientId = _clientId,
                Username = username,
                ConfirmationCode = request.ConfirmationCode
            };

            // Add SECRET_HASH if client secret exists
            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                confirmRequest.SecretHash = ComputeSecretHash(username);
            }

            await _cognitoClient.ConfirmSignUpAsync(confirmRequest);

            return (true, "Email confirmed successfully. You can now login.");
        }
        catch (CodeMismatchException)
        {
            return (false, "Invalid confirmation code.");
        }
        catch (ExpiredCodeException)
        {
            return (false, "Confirmation code has expired. Please request a new one.");
        }
        catch (UserNotFoundException ex)
        {
            Console.WriteLine($"[EmailService.ConfirmEmailAsync] User not found in Cognito: {ex.Message}");
            return (false, "User not found in Cognito.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService.ConfirmEmailAsync] Error: {ex.Message}");
            return (false, $"Confirmation failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
            {
                Console.WriteLine($"[EmailService.ResendConfirmationAsync] User not found for email: {request.Email}");
                return (true, "If the email exists, a confirmation code has been sent.");
            }

            var resendRequest = new ResendConfirmationCodeRequest
            {
                ClientId = _clientId,
                Username = username
            };

            // Add SECRET_HASH if client secret exists
            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                resendRequest.SecretHash = ComputeSecretHash(username);
            }

            await _cognitoClient.ResendConfirmationCodeAsync(resendRequest);

            return (true, "Confirmation code resent. Please check your email.");
        }
        catch (UserNotFoundException ex)
        {
            Console.WriteLine($"[EmailService.ResendConfirmationAsync] User not found in Cognito: {ex.Message}");
            return (true, "If the email exists, a confirmation code has been sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService.ResendConfirmationAsync] Error: {ex.Message}");
            return (false, $"Failed to resend code: {ex.Message}");
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
            Console.WriteLine($"[EmailService.FindUsernameByEmail] Searching for email: {email}");

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
                Console.WriteLine($"[EmailService.FindUsernameByEmail] Found user via ListUsers: {user.Username}");
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
                Console.WriteLine($"[EmailService.FindUsernameByEmail] Found user via AdminGetUser: {adminResponse.Username}");
                return adminResponse.Username;
            }
            catch (UserNotFoundException)
            {
                Console.WriteLine($"[EmailService.FindUsernameByEmail] User not found with email as username: {email}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService.FindUsernameByEmail] Error for {email}: {ex.Message}");
            return null;
        }
    }
}
