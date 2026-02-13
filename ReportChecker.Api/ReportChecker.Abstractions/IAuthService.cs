using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAuthService
{
    public string GetAuthUrl(string provider, string redirectUrl);
    public Task<UserCredentials> GetTokenAsync(string code);
    public Task<UserCredentials> RefreshTokenAsync(string refreshToken);
}