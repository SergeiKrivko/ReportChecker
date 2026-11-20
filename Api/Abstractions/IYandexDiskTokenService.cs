using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Abstractions;

/// <summary>
/// Доступ к OAuth-токену Яндекс.Диска. Токен хранится в Avalux Auth вместе с
/// привязанным Яндекс-аккаунтом и обновляется сервисом авторизации автоматически.
/// </summary>
public interface IYandexDiskTokenService
{
    /// <summary>Есть ли у пользователя привязанный Яндекс-аккаунт и действующий токен Диска.</summary>
    public Task<YandexDiskConnection> GetConnectionAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Действующий токен Диска. Если Яндекс не привязан или токен не выдан —
    /// <see cref="Exceptions.UnauthorizedException"/>.
    /// </summary>
    public Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct = default);
}
