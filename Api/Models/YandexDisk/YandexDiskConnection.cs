namespace ReportChecker.Models.YandexDisk;

public class YandexDiskConnection
{
    /// <summary>Привязан ли Яндекс-аккаунт в Avalux Auth.</summary>
    public bool HasAccount { get; init; }

    /// <summary>Можно ли работать с Диском: аккаунт привязан и токен выдан.</summary>
    public bool Connected { get; init; }

    public string? Login { get; init; }
}
