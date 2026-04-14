using System.Runtime.Versioning;
using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Abstractions;

public interface IAuthService
{
    public IReadOnlyList<AuthProvider> GetProviders();
    public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("Macos")]
    public Task AuthenticateAsync(AuthProvider provider, CancellationToken ct = default);

    public Task<User> GetUserAsync(CancellationToken ct = default);
}