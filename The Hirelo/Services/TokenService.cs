using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Security.Cryptography;
using System.Text;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Services;

public class TokenService : ITokenService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _clientId;
    private readonly string? _clientSecret;

    public TokenService(
        IAmazonCognitoIdentityProvider cognitoClient,
        IConfiguration configuration)
    {
        _cognitoClient = cognitoClient;
        _clientId = configuration["AWS:Cognito:ClientId"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientId")!;
        _clientSecret = configuration["AWS:Cognito:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret");
    }

    public async Task<(bool Success, string AccessToken, string? IdToken, int ExpiresIn, string? TokenType)> RefreshTokenAsync(RefreshTokenRequest request)
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

            // Add SECRET_HASH if client secret exists
            if (!string.IsNullOrWhiteSpace(_clientSecret))
            {
                authRequest.AuthParameters["SECRET_HASH"] = ComputeSecretHash("refresh_token_hash");
            }

            var response = await _cognitoClient.InitiateAuthAsync(authRequest);
            var authResult = response.AuthenticationResult;

            return (true, authResult.AccessToken, authResult.IdToken, authResult.ExpiresIn ?? 0, authResult.TokenType);
        }
        catch (NotAuthorizedException)
        {
            return (false, "", null, 0, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenService.RefreshTokenAsync] Error: {ex.Message}");
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
}
