using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Abstractions;

public interface IYandexDiskTokenService
{
    /// <summary>Ссылка на страницу согласия Яндекс OAuth.</summary>
    public string CreateAuthorizationUrl(string state);

    /// <summary>Обменивает код авторизации на токены и сохраняет подключение.</summary>
    public Task ConnectAsync(Guid userId, string code, CancellationToken ct = default);

    public Task<YandexDiskConnection> GetConnectionAsync(Guid userId, CancellationToken ct = default);

    public Task DisconnectAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Действующий access-токен; обновляет его при истечении, если есть refresh-токен.</summary>
    public Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct = default);
}
