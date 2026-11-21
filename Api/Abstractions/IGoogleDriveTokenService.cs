using ReportChecker.Models.GoogleDrive;

namespace ReportChecker.Abstractions;

/// <summary>
/// Доступ к OAuth-токену Google Диска. Токен хранится в Avalux Auth вместе с
/// привязанным Google-аккаунтом и обновляется сервисом авторизации автоматически.
/// </summary>
public interface IGoogleDriveTokenService
{
    /// <summary>Есть ли у пользователя привязанный Google-аккаунт и действующий токен Диска.</summary>
    public Task<GoogleDriveConnection> GetConnectionAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Действующий токен Диска. Если Google не привязан или токен не выдан —
    /// <see cref="Exceptions.UnauthorizedException"/>.
    /// </summary>
    public Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct = default);
}
