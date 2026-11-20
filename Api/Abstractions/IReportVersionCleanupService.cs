namespace ReportChecker.Abstractions;

/// <summary>
/// Удаляет из S3 файлы старых версий отчёта. Записи о версиях в БД сохраняются.
/// </summary>
public interface IReportVersionCleanupService
{
    /// <summary>
    /// Удаляет файлы всех версий отчёта, кроме последней и последней успешной.
    /// </summary>
    public Task CleanupOldVersionsAsync(Guid reportId, CancellationToken ct = default);
}
