using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models.YandexDisk;

namespace ReportChecker.SourceProviders.YandexDisk;

public class YandexDiskTokenService(
    IYandexDiskAccountRepository repository,
    YandexDiskOAuthClient oauthClient) : IYandexDiskTokenService
{
    public string CreateAuthorizationUrl(string state)
    {
        return oauthClient.BuildAuthorizationUrl(state);
    }

    public async Task ConnectAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var token = await oauthClient.ExchangeCodeAsync(code, ct);
        var login = await oauthClient.TryGetLoginAsync(token.AccessToken!, ct);

        await repository.SaveAsync(new YandexDiskAccount
        {
            UserId = userId,
            Login = login,
            AccessToken = token.AccessToken!,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.ExpiresIn == null ? null : DateTime.UtcNow.AddSeconds(token.ExpiresIn.Value),
        }, ct);
    }

    public async Task<YandexDiskConnection> GetConnectionAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await repository.GetByUserIdAsync(userId, ct);
        return new YandexDiskConnection
        {
            Connected = account != null,
            Login = account?.Login,
        };
    }

    public Task DisconnectAsync(Guid userId, CancellationToken ct = default)
    {
        return repository.DeleteAsync(userId, ct);
    }

    public async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await repository.GetByUserIdAsync(userId, ct) ??
                      throw new UnauthorizedException(
                          "Яндекс.Диск не подключён. Подключите его в настройках источника отчёта");

        if (account.ExpiresAt == null || account.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return account.AccessToken;

        if (account.RefreshToken == null)
            throw new UnauthorizedException("Срок действия токена Яндекс.Диска истёк. Подключите Диск заново");

        var refreshed = await oauthClient.RefreshAsync(account.RefreshToken, ct);
        if (refreshed == null)
        {
            await repository.DeleteAsync(userId, ct);
            throw new UnauthorizedException("Не удалось обновить токен Яндекс.Диска. Подключите Диск заново");
        }

        var updated = new YandexDiskAccount
        {
            UserId = account.UserId,
            Login = account.Login,
            AccessToken = refreshed.AccessToken!,
            RefreshToken = refreshed.RefreshToken ?? account.RefreshToken,
            ExpiresAt = refreshed.ExpiresIn == null
                ? null
                : DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn.Value),
        };
        await repository.SaveAsync(updated, ct);
        return updated.AccessToken;
    }
}
