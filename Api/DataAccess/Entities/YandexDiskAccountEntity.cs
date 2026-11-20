using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

/// <summary>
/// Подключение пользователя к Яндекс.Диску. Токены хранятся в зашифрованном
/// виде (<see cref="Abstractions.IYandexDiskTokenProtector"/>).
/// </summary>
public class YandexDiskAccountEntity
{
    public Guid UserId { get; init; }
    [MaxLength(256)] public string? Login { get; init; }
    [MaxLength(4096)] public required string AccessToken { get; init; }
    [MaxLength(4096)] public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
