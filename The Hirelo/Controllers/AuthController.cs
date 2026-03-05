using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using The_Hirelo.Data;
using The_Hirelo.Models;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using ChangePasswordRequest = The_Hirelo.DTOs.Requests.ChangePasswordRequest;
using The_Hirelo.Enums;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly HireloDbContext _context;
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _clientId;
    private readonly string _userPoolId;
    private readonly string? _clientSecret;

    public AuthController(
        HireloDbContext context,
        IAmazonCognitoIdentityProvider cognitoClient,
        IConfiguration configuration)
    {
        _context = context;
        _cognitoClient = cognitoClient;
        _clientId = configuration["AWS:Cognito:ClientId"]!;
        _userPoolId = configuration["AWS:Cognito:UserPoolId"]!;
        _clientSecret = configuration["AWS:Cognito:ClientSecret"];
    }

    private string ComputeSecretHash(string username)
    {
        if (string.IsNullOrEmpty(_clientSecret)) return string.Empty;
        var data = Encoding.UTF8.GetBytes(username + _clientId);
        var key = Encoding.UTF8.GetBytes(_clientSecret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if email already exists in database
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            // Use a generated username by default. If pool requires email as username we'll retry.
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

            return Ok(new
            {
                message = "Registration successful. Please check your email for a confirmation code.",
                username = response.UserConfirmed == true ? request.Email : null,
                userSub = response.UserSub,
                confirmed = response.UserConfirmed
            });
        }
        catch (UsernameExistsException)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }
        catch (InvalidPasswordException ex)
        {
            return BadRequest(new { message = $"Invalid password: {ex.Message}" });
        }
        catch (InvalidParameterException ex)
        {
            return BadRequest(new { message = $"Invalid parameter: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Registration failed: {ex.Message}" });
        }
    }

    // POST /api/auth/confirm-email
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
                return NotFound(new { message = "User not found." });

            var confirmRequest = new ConfirmSignUpRequest
            {
                ClientId = _clientId,
                Username = username,
                ConfirmationCode = request.ConfirmationCode
            };

            await _cognitoClient.ConfirmSignUpAsync(confirmRequest);

            return Ok(new { message = "Email confirmed successfully. You can now login." });
        }
        catch (CodeMismatchException)
        {
            return BadRequest(new { message = "Invalid confirmation code." });
        }
        catch (ExpiredCodeException)
        {
            return BadRequest(new { message = "Confirmation code has expired. Please request a new one." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Confirmation failed: {ex.Message}" });
        }
    }

    // POST /api/auth/resend-confirmation
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
                return Ok(new { message = "If the email exists, a confirmation code has been sent." });

            var resendRequest = new ResendConfirmationCodeRequest
            {
                ClientId = _clientId,
                Username = username
            };

            await _cognitoClient.ResendConfirmationCodeAsync(resendRequest);

            return Ok(new { message = "Confirmation code resent. Please check your email." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to resend code: {ex.Message}" });
        }
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
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
                return Ok(new
                {
                    challengeName = response.ChallengeName.Value,
                    session = response.Session,
                    message = $"Challenge required: {response.ChallengeName.Value}"
                });
            }

            var authResult = response.AuthenticationResult;

            var cognitoSub = await GetCognitoSub(request.Email);
            if (!string.IsNullOrEmpty(cognitoSub))
            {
                await SyncUserToDatabase(cognitoSub, request.Email);
            }

            return Ok(new LoginResponse
            {
                AccessToken = authResult.AccessToken,
                IdToken = authResult.IdToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresIn = authResult.ExpiresIn ?? 0,
                TokenType = authResult.TokenType
            });
        }
        catch (NotAuthorizedException)
        {
            return Unauthorized(new { message = "Incorrect email or password." });
        }
        catch (UserNotConfirmedException)
        {
            return BadRequest(new { message = "Email not confirmed. Please confirm your email first." });
        }
        catch (UserNotFoundException)
        {
            return NotFound(new { message = "User not found." });
        }
        catch (InvalidParameterException ex)
        {
            return BadRequest(new { message = $"Invalid parameter: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Login failed: {ex.Message}" });
        }
    }

    // POST /api/auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var authRequest = new InitiateAuthRequest
            {
                ClientId = _clientId,
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", request.RefreshToken }
                }
            };

            var response = await _cognitoClient.InitiateAuthAsync(authRequest);
            var authResult = response.AuthenticationResult;

            return Ok(new
            {
                accessToken = authResult.AccessToken,
                idToken = authResult.IdToken,
                expiresIn = authResult.ExpiresIn,
                tokenType = authResult.TokenType
            });
        }
        catch (NotAuthorizedException)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Token refresh failed: {ex.Message}" });
        }
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] DTOs.Requests.ForgotPasswordRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
            {
                // Don't reveal if user exists
                return Ok(new { message = "If the email exists, a reset code has been sent." });
            }

            var forgotRequest = new Amazon.CognitoIdentityProvider.Model.ForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = username
            };

            await _cognitoClient.ForgotPasswordAsync(forgotRequest);

            return Ok(new { message = "Password reset code sent to your email." });
        }
        catch (UserNotFoundException)
        {
            return Ok(new { message = "If the email exists, a reset code has been sent." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to initiate password reset: {ex.Message}" });
        }
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var username = await FindUsernameByEmail(request.Email);
            if (username == null)
                return NotFound(new { message = "User not found." });

            var confirmRequest = new ConfirmForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = username,
                ConfirmationCode = request.ConfirmationCode,
                Password = request.NewPassword
            };

            await _cognitoClient.ConfirmForgotPasswordAsync(confirmRequest);

            return Ok(new { message = "Password reset successfully. You can now login with your new password." });
        }
        catch (CodeMismatchException)
        {
            return BadRequest(new { message = "Invalid reset code." });
        }
        catch (ExpiredCodeException)
        {
            return BadRequest(new { message = "Reset code has expired. Please request a new one." });
        }
        catch (InvalidPasswordException ex)
        {
            return BadRequest(new { message = $"Invalid password: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Password reset failed: {ex.Message}" });
        }
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var accessToken = HttpContext.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            var changeRequest = new Amazon.CognitoIdentityProvider.Model.ChangePasswordRequest
            {
                AccessToken = accessToken,
                PreviousPassword = request.CurrentPassword,
                ProposedPassword = request.NewPassword
            };

            await _cognitoClient.ChangePasswordAsync(changeRequest);

            return Ok(new { message = "Password changed successfully." });
        }
        catch (NotAuthorizedException)
        {
            return Unauthorized(new { message = "Current password is incorrect." });
        }
        catch (InvalidPasswordException ex)
        {
            return BadRequest(new { message = $"Invalid new password: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Password change failed: {ex.Message}" });
        }
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var accessToken = HttpContext.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            var signOutRequest = new GlobalSignOutRequest
            {
                AccessToken = accessToken
            };

            await _cognitoClient.GlobalSignOutAsync(signOutRequest);

            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Logout failed: {ex.Message}" });
        }
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
    {
        var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(cognitoSub))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);

        if (user == null)
        {
            return NotFound(new { message = "User not found. Please login again." });
        }

        return Ok(new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    // PUT /api/auth/users/{userId}/role
    [HttpPut("users/{userId}/role")]
    [Authorize]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var currentCognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.CognitoSub == currentCognitoSub);

        if (currentUser == null || currentUser.Role != UserRole.Admin)
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var validRoles = new[] { UserRole.Admin, UserRole.Recruiter, UserRole.Candidate };
        if (!validRoles.Contains(request.Role))
        {
            return BadRequest(new { message = "Invalid role. Valid roles: Admin, Recruiter, Candidate" });
        }

        user.Role = request.Role;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Role updated successfully" });
    }

    // Private helpers
    private async Task<string?> FindUsernameByEmail(string email)
    {
        try
        {
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
                return user.Username;

            // If not found by ListUsers, try using email as username directly (email might be the username)
            try
            {
                var adminGetRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };
                var adminResponse = await _cognitoClient.AdminGetUserAsync(adminGetRequest);
                return adminResponse.Username;
            }
            catch
            {
                // Email is not the username, return null
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FindUsernameByEmail error for {email}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetCognitoSub(string email)
    {
        try
        {
            var listRequest = new ListUsersRequest
            {
                UserPoolId = _userPoolId,
                Filter = $"email = \"{email}\"",
                Limit = 1
            };

            var response = await _cognitoClient.ListUsersAsync(listRequest);
            var user = response.Users?.FirstOrDefault();

            if (user != null)
                return user?.Attributes?.FirstOrDefault(a => a.Name == "sub")?.Value;

            // Fallback: try email as username
            try
            {
                var adminGetRequest = new AdminGetUserRequest
                {
                    UserPoolId = _userPoolId,
                    Username = email
                };
                var adminResponse = await _cognitoClient.AdminGetUserAsync(adminGetRequest);
                return adminResponse.UserAttributes?.FirstOrDefault(a => a.Name == "sub")?.Value;
            }
            catch
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCognitoSub error for {email}: {ex.Message}");
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

// DTOs for testing purposes

//public class RegisterRequest
//{
//    public string Email { get; set; } = null!;
//    public string Password { get; set; } = null!;
//    public string FullName { get; set; } = null!;
//}

//public class ConfirmEmailRequest
//{
//    public string Email { get; set; } = null!;
//    public string ConfirmationCode { get; set; } = null!;
//}

//public class ResendConfirmationRequest
//{
//    public string Email { get; set; } = null!;
//}

//public class LoginRequest
//{
//    public string Email { get; set; } = null!;
//    public string Password { get; set; } = null!;
//}

//public class LoginResponse
//{
//    public string AccessToken { get; set; } = null!;
//    public string IdToken { get; set; } = null!
//    public string RefreshToken { get; set; } = null!;
//    public int ExpiresIn { get; set; }
//    public string TokenType { get; set; } = null!;
//}

//public class RefreshTokenRequest
//{
//    public string RefreshToken { get; set; } = null!;
//}

//public class ForgotPasswordRequestDto
//{
//    public string Email { get; set; } = null!;
//}

//public class ResetPasswordRequest
//{
//    public string Email { get; set; } = null!;
//    public string ConfirmationCode { get; set; } = null!;
//    public string NewPassword { get; set; } = null!;
//}

//public class ChangePasswordRequest
//{
//    public string CurrentPassword { get; set; } = null!;
//    public string NewPassword { get; set; } = null!;
//}

//public class UserInfoResponse
//{
//    public Guid Id { get; set; }
//    public string Email { get; set; } = null!;
//    public string Role { get; set; } = null!;
//    public DateTime CreatedAt { get; set; }
//}

//public class UpdateRoleRequest
//{
//    public string Role { get; set; } = null!;
//}
