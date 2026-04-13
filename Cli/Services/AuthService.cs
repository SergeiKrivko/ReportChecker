using System.Runtime.Versioning;
using Avalux.Auth.UserClient;
using AvaluxUI.Utils;
using Microsoft.Extensions.Configuration;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Services;

internal class AuthService(IAuthClient authClient, ISettingsSection settings, IConfiguration configuration) : IAuthService
{
    private const string CallbackUrl = "http://localhost:14872";

    public IReadOnlyList<AuthProvider> GetProviders()
    {
        return
        [
            new AuthProvider("password", "Логин и пароль"),
            new AuthProvider("yandex", "Яндекс"),
            new AuthProvider("google", "Google"),
            new AuthProvider("github", "GitHub"),
            new AuthProvider("gitlab", "GitLab"),
            new AuthProvider("microsoft", "Microsoft"),
        ];
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        await authClient.RefreshTokenAsync(ct: ct);
        return authClient.IsAuthenticated;
    }

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("Macos")]
    public async Task AuthenticateAsync(AuthProvider provider, CancellationToken ct = default)
    {
        await authClient.AuthorizeInstalledAsync(provider.Key, CallbackUrl, ct);
    }
}