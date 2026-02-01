using System.Security.Claims;
using ReportChecker.Abstractions;
using ReportChecker.AuthProviders.Yandex;

namespace ReportChecker.Application.Services;

public class AuthService(
    IAccountRepository accountRepository,
    IUserRepository userRepository,
    YandexAuthProvider yandexAuthProvider) : IAuthService
{
    private readonly IAuthProvider[] _authProviders = [yandexAuthProvider];

    public async Task<Guid?> AuthenticateAsync(ClaimsPrincipal principal)
    {
        var provider = _authProviders.First(p => p.VerifyProvider(principal));
        var accountInfo = await provider.GetAccountInfoAsync(principal);
        if (accountInfo == null)
            return null;
        var account = await accountRepository.GetAccountByProviderIdAsync(accountInfo.Id);
        if (account == null || account.DeletedAt != null)
            return null;
        return account.UserId;
    }

    public async Task<Guid?> AuthenticateOrCreateUserAsync(ClaimsPrincipal principal)
    {
        var provider = _authProviders.First(p => p.VerifyProvider(principal));
        var accountInfo = await provider.GetAccountInfoAsync(principal);
        if (accountInfo == null)
            return null;
        var account = await accountRepository.GetAccountByProviderIdAsync(accountInfo.Id);
        if (account != null && account.DeletedAt == null)
            return account.UserId;
        var userId = await userRepository.CreateUserAsync();
        await accountRepository.CreateAccountAsync(userId, provider.Key, accountInfo.Id);
        return userId;
    }

    public IAuthProvider? GetAuthProvider(string key)
    {
        return _authProviders.FirstOrDefault(e => e.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
    }
}