using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class AuthService(
    IAccountRepository accountRepository,
    IProviderService providerService,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository) : IAuthService
{
    public const string Issuer = "ReportChecker";
    public const string Audience = "ReportCheckerAccessToken";

    public static SymmetricSecurityKey SecurityKey { get; set; } =
        new(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET") ?? ""));
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);

    public Uri GetAuthorizationUrl(string providerKey, string redirectUrl)
    {
        var provider = providerService.GetAuthProvider(providerKey);
        return provider.GetAuthorizationUrl(redirectUrl);
    }

    public async Task<Guid> AuthorizeAsync(string providerKey, Dictionary<string, string> parameters,
        string? redirectUrl)
    {
        var provider = providerService.GetAuthProvider(providerKey);
        var credentials = await provider.AuthorizeAsync(parameters, redirectUrl);

        var existingAccount = await accountRepository.GetAccountByProviderIdAsync(providerKey, credentials.Id);
        if (existingAccount != null)
        {
            await accountRepository.UpdateAccountCredentialsAsync(existingAccount.Id, credentials.Credentials);
            return existingAccount.Id;
        }
        var userId = await userRepository.CreateUserAsync();
        await accountRepository.CreateAccountAsync(userId, providerKey, credentials.Id, credentials.Credentials);
        return userId;
    }

    public async Task AuthorizeAsync(Guid userId, string providerKey, Dictionary<string, string> parameters,
        string? redirectUrl)
    {
        var provider = providerService.GetAuthProvider(providerKey);
        var credentials = await provider.AuthorizeAsync(parameters, redirectUrl);

        var existingAccount = await accountRepository.GetAccountByProviderIdAsync(providerKey, credentials.Id);
        if (existingAccount != null)
            await accountRepository.UpdateAccountCredentialsAsync(existingAccount.Id, credentials.Credentials);
        else
            await accountRepository.CreateAccountAsync(userId, providerKey, credentials.Id, credentials.Credentials);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[40];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static JwtSecurityToken GenerateAccessToken(Guid userId)
    {
        return new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("UserId", userId.ToString())],
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256)
        );
    }

    public async Task<TokenPair> CreateTokenPairAsync(Guid userId)
    {
        var refreshToken = GenerateRefreshToken();
        var accessToken = GenerateAccessToken(userId);
        await refreshTokenRepository.CreateRefreshTokenAsync(userId, refreshToken);
        return new TokenPair
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
            RefreshToken = refreshToken,
            ExpiresAt = accessToken.ValidTo,
        };
    }

    public async Task<TokenPair?> RefreshTokenAsync(string refreshToken)
    {
        var userId = await refreshTokenRepository.GetUserIdByRefreshTokenAsync(refreshToken);
        if (userId == null)
            return null;
        var newRefreshToken = GenerateRefreshToken();
        var accessToken = GenerateAccessToken(userId.Value);
        await refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken, newRefreshToken);
        return new TokenPair
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
            RefreshToken = refreshToken,
            ExpiresAt = accessToken.ValidTo,
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        await refreshTokenRepository.DeleteRefreshTokenAsync(refreshToken);
    }
}