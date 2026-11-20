namespace ReportChecker.Models.YandexDisk;

/// <summary>Подключение пользователя к Яндекс.Диску (OAuth-токены хранятся в зашифрованном виде).</summary>
public class YandexDiskAccount
{
    public required Guid UserId { get; init; }
    public string? Login { get; init; }
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public class YandexDiskConnection
{
    public bool Connected { get; init; }
    public string? Login { get; init; }
}
