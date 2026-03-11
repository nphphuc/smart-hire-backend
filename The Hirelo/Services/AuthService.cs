using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;
using The_Hirelo.Models;
using The_Hirelo.Services.Interfaces;


namespace The_Hirelo.Services;

public class AuthService : IAuthService
{
    private readonly HireloDbContext _context;
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _clientId;
    private readonly string _userPoolId;
    private readonly string? _clientSecret;

    public AuthService(
        HireloDbContext context,
        IAmazonCognitoIdentityProvider cognitoClient,
        IConfiguration configuration)
    {
        _context = context;
        _cognitoClient = cognitoClient;
        _clientId = configuration["AWS:Cognito:ClientId"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientId")!;
        _userPoolId = configuration["AWS:Cognito:UserPoolId"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId")!;
        _clientSecret = configuration["AWS:Cognito:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret");
    }

    public async Task<(bool Success, string Message, string? UserSub)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if email already exists in database
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return (false, "A user with this email already exists.", null);
            }

            // Use a generated username by default
            var username = Guid.NewGuid().ToString();

            SignUpRequest CreateSignUp(string user)
            {
                var req = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = user,
                    Password = request.Password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType { Name = "email", Value = request.Email },
                        new AttributeType { Name = "name", Value = request.FullName }
                    }
                };

                if (!string.IsNullOrWhiteSpace(_clientSecret))
                {
                    req.SecretHash = ComputeSecretHash(user);
                }

                return req;
            }

            var signUpRequest = CreateSignUp(username);

            SignUpResponse response;
            try
            {
                response = await _cognitoClient.SignUpAsync(signUpRequest);
            }
            catch (InvalidParameterException ex)
            {
                // If Cognito requires username to be an email, retry with the email as username
                var msg = ex.Message ?? string.Empty;
                if (msg.Contains("Username should be an email", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("Username must be an email", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("Username cannot be of email format", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("email", StringComparison.OrdinalIgnoreCase))
                {
                    var emailUsernameRequest = CreateSignUp(request.Email);
                    response = await _cognitoClient.SignUpAsync(emailUsernameRequest);
                }
                else
                {
                    throw;
                }
            }

            // Save user to database after successful signup
            await SyncUserToDatabase(response.UserSub, request.Email);

            return (true, "Registration successful. Please check your email for a confirmation code.", response.UserSub);
        }
        catch (UsernameExistsException)
        {
            return (false, "A user with this email already exists.", null);
        }
        catch (InvalidPasswordException ex)
        {
            return (false, $"Invalid password: {ex.Message}", null);
        }
        catch (InvalidParameterException ex)
        {
            return (false, $"Invalid parameter: {ex.Message}", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService.RegisterAsync] Error: {ex.Message}");
            return (false, $"Registration failed: {ex.Message}", null);
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var authRequest = new InitiateAuthRequest
            {
                ClientId = _clientId,
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", request.Email },
                    { "PASSWORD", request.Password }
                }
            };

            // Add SECRET_HASH if client secret exists
            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                authRequest.AuthParameters["SECRET_HASH"] = ComputeSecretHash(request.Email);
            }

            var response = await _cognitoClient.InitiateAuthAsync(authRequest);

            if (response.ChallengeName != null && response.ChallengeName.Value != null)
            {
                throw new InvalidOperationException($"Challenge required: {response.ChallengeName.Value}");
            }

            var authResult = response.AuthenticationResult;

            var cognitoSub = await GetCognitoSub(request.Email);
            if (!string.IsNullOrEmpty(cognitoSub))
            {
                await SyncUserToDatabase(cognitoSub, request.Email);
            }

            return new LoginResponse
            {
                AccessToken = authResult.AccessToken,
                IdToken = authResult.IdToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresIn = authResult.ExpiresIn ?? 0,
                TokenType = authResult.TokenType
            };
        }
        catch (NotAuthorizedException)
        {
            throw new UnauthorizedAccessException("Incorrect email or password.");
        }
        catch (UserNotConfirmedException)
        {
            throw new InvalidOperationException("Email not confirmed. Please confirm your email first.");
        }
        catch (UserNotFoundException)
        {
            throw new InvalidOperationException("User not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService.LoginAsync] Error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> LogoutAsync(string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            var signOutRequest = new GlobalSignOutRequest
            {
                AccessToken = accessToken
            };

            await _cognitoClient.GlobalSignOutAsync(signOutRequest);
            return true;
        }
        catch (NotAuthorizedException)
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService.LogoutAsync] Error: {ex.Message}");
            throw;
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
            Console.WriteLine($"[AuthService.FindUsernameByEmail] Searching for email: {email}");

            // Try using ListUsers with email filter
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
                Console.WriteLine($"[AuthService.FindUsernameByEmail] Found user via ListUsers: {user.Username}");
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
                Console.WriteLine($"[AuthService.FindUsernameByEmail] Found user via AdminGetUser: {adminResponse.Username}");
                return adminResponse.Username;
            }
            catch (UserNotFoundException)
            {
                Console.WriteLine($"[AuthService.FindUsernameByEmail] User not found with email as username: {email}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService.FindUsernameByEmail] Error for {email}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetCognitoSub(string email)
    {
        try
        {
            Console.WriteLine($"[AuthService.GetCognitoSub] Getting sub for email: {email}");

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
                var sub = user?.Attributes?.FirstOrDefault(a => a.Name == "sub")?.Value;
                Console.WriteLine($"[AuthService.GetCognitoSub] Found sub via ListUsers: {sub}");
                return sub;
            }

            // Fallback: try email as username
            try
            {
                var adminGetRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };
                var adminResponse = await _cognitoClient.AdminGetUserAsync(adminGetRequest);
                var sub = adminResponse.UserAttributes?.FirstOrDefault(a => a.Name == "sub")?.Value;
                Console.WriteLine($"[AuthService.GetCognitoSub] Found sub via AdminGetUser: {sub}");
                return sub;
            }
            catch (UserNotFoundException)
            {
                Console.WriteLine($"[AuthService.GetCognitoSub] User not found with email as username: {email}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService.GetCognitoSub] Error for {email}: {ex.Message}");
            return null;
        }
    }

    private async Task SyncUserToDatabase(string cognitoSub, string email)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);

        if (existingUser == null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                CognitoSub = cognitoSub,
                Email = email,
                Role = UserRole.Candidate,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else if (existingUser.Email != email)
        {
            existingUser.Email = email;
            await _context.SaveChangesAsync();
        }
    }
}
