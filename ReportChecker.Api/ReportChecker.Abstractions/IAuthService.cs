using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAuthService
{
    public Uri GetAuthorizationUrl(string provider, string redirectUrl);
    public Task<Guid> AuthorizeAsync(string provider, Dictionary<string, string> parameters, string? redirectUrl);
    public Task AuthorizeAsync(Guid userId, string provider, Dictionary<string, string> parameters, string? redirectUrl);
    public Task<TokenPair> CreateTokenPairAsync(Guid userId);
    public Task<TokenPair?> RefreshTokenAsync(string refreshToken);
    public Task RevokeTokenAsync(string refreshToken);
}