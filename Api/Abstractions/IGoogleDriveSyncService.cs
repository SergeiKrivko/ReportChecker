namespace ReportChecker.Abstractions;

public class GoogleDriveSyncResult
{
    public bool Created { get; init; }
    public Guid? CheckId { get; init; }

    /// <summary>Почему проверка не создана (для логов и UI).</summary>
    public string? Reason { get; init; }
}

public interface IGoogleDriveSyncService
{
    /// <summary>
    /// Сверяет текущее состояние файла на Диске с последней проверкой и при изменении
    /// создаёт новую. <paramref name="force"/> игнорирует совпадение отпечатков.
    /// </summary>
    public Task<GoogleDriveSyncResult> SyncAsync(Guid reportId, bool force, CancellationToken ct = default);
}
