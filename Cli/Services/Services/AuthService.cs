using System.Runtime.Versioning;
using Avalux.Auth.UserClient;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Services.Services;

internal class AuthService(IAuthClient authClient) : IAuthService
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
        try
        {
            await authClient.RefreshTokenAsync(ct: ct);
        }
        catch (Exception)
        {
            return false;
        }
        return authClient.IsAuthenticated;
    }

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("Macos")]
    public async Task AuthenticateAsync(AuthProvider provider, CancellationToken ct = default)
    {
        await authClient.AuthorizeInstalledAsync(provider.Key, CallbackUrl, ct);
    }

    public async Task<User> GetUserAsync(CancellationToken ct = default)
    {
        var res = await authClient.GetUserInfoAsync(ct);
        return new User
        {
            Id = res.Id,
            Accounts = res.Accounts.Select(e => new AccountInfo
            {
                Id = e.Id,
                Login = e.Login,
                Name = e.Name,
                Provider = e.Provider,
                AvatarUrl = e.AvatarUrl,
            }).ToList(),
        };
    }
}