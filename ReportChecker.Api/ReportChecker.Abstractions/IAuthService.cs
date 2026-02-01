using System.Security.Claims;

namespace ReportChecker.Abstractions;

public interface IAuthService
{
    public IAuthProvider? GetAuthProvider(string key);
    public Task<Guid?> AuthenticateAsync(ClaimsPrincipal principal);
    public Task<Guid?> AuthenticateOrCreateUserAsync(ClaimsPrincipal principal);
}