using System.Runtime.Versioning;
using ReportChecker.Shared.Models;

namespace ReportChecker.Shared.Abstractions;

public interface IAuthService
{
    public IReadOnlyList<AuthProvider> GetProviders();
    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("Macos")]
    public Task AuthenticateAsync(AuthProvider provider, CancellationToken ct = default);

    public Task<User> GetUserAsync(CancellationToken ct = default);
    public Task LogOutAsync(CancellationToken ct = default);
}