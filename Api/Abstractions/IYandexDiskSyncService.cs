namespace ReportChecker.Abstractions;

public class YandexDiskSyncResult
{
    public bool Created { get; init; }
    public Guid? CheckId { get; init; }

    /// <summary>Почему проверка не создана (для логов и UI).</summary>
    public string? Reason { get; init; }
}

public interface IYandexDiskSyncService
{
    /// <summary>
    /// Сверяет текущее состояние файла на Диске с последней проверкой и при изменении
    /// создаёт новую. <paramref name="force"/> игнорирует совпадение отпечатков.
    /// </summary>
    public Task<YandexDiskSyncResult> SyncAsync(Guid reportId, bool force, CancellationToken ct = default);
}
