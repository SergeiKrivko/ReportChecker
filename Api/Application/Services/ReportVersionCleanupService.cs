using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Application.Services;

/// <summary>
/// Удаляет из S3 файлы старых версий отчёта (проверок), сохраняя две версии:
/// последнюю и последнюю успешную. Записи в БД не изменяются — файлы просто
/// перестают занимать место в хранилище.
/// </summary>
public class ReportVersionCleanupService(
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    IReportSourceRepository<FileReportSource> fileReportSourceRepository,
    IReportSourceRepository<LocalReportSource> localReportSourceRepository,
    ICheckSourceRepository<FileCheckSource> fileCheckSourceRepository,
    ICheckSourceRepository<LocalCheckSource> localCheckSourceRepository,
    IFileRepository fileRepository,
    ILogger<ReportVersionCleanupService> logger) : IReportVersionCleanupService
{
    public async Task CleanupOldVersionsAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId, ct);
        if (report is null)
            return;

        var checks = (await checkRepository.GetAllChecksOfReportAsync(reportId, ct)).ToList();
        if (checks.Count == 0)
            return;

        var keptCheckIds = GetCheckIdsToKeep(checks);

        // Версии источника GitHub хранятся в репозитории, а не в S3.
        if (string.Equals(report.SourceProvider, "File", StringComparison.OrdinalIgnoreCase))
        {
            var initialFileId = (await fileReportSourceRepository.GetByReportIdAsync(reportId, ct))
                ?.Data.InitialFileId;
            await DeleteOldSourcesAsync(fileCheckSourceRepository, FileRepositoryBucket.Sources, checks,
                keptCheckIds, initialFileId, e => e.FileName, reportId, ct);
        }
        else if (string.Equals(report.SourceProvider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            var initialFileId = (await localReportSourceRepository.GetByReportIdAsync(reportId, ct))
                ?.Data.InitialFileId;
            await DeleteOldSourcesAsync(localCheckSourceRepository, FileRepositoryBucket.LocalSources, checks,
                keptCheckIds, initialFileId, e => e.FileName, reportId, ct);
        }
    }

    /// <summary>
    /// Последняя версия — самая новая проверка (в любом статусе), последняя успешная —
    /// самая новая проверка со статусом <see cref="ProgressStatus.Completed"/>.
    /// </summary>
    private static HashSet<Guid> GetCheckIdsToKeep(IEnumerable<Check> checks)
    {
        var ordered = checks.OrderByDescending(e => e.CreatedAt).ToList();
        var latest = ordered[0];
        var latestSuccessful = ordered.FirstOrDefault(e => e.Status == ProgressStatus.Completed);

        var result = new HashSet<Guid> { latest.Id };
        if (latestSuccessful is not null)
            result.Add(latestSuccessful.Id);
        return result;
    }

    private async Task DeleteOldSourcesAsync<TSource>(
        ICheckSourceRepository<TSource> checkSourceRepository,
        FileRepositoryBucket bucket,
        IEnumerable<Check> checks,
        IReadOnlySet<Guid> keptCheckIds,
        Guid? initialFileId,
        Func<TSource, string?> fileNameSelector,
        Guid reportId,
        CancellationToken ct) where TSource : class
    {
        var deletedCount = 0;
        foreach (var check in checks)
        {
            ct.ThrowIfCancellationRequested();
            if (keptCheckIds.Contains(check.Id))
                continue;

            var source = await checkSourceRepository.GetByCheckIdAsync(check.Id, ct);
            if (source is null)
                continue;

            // На файл из источника отчёта ссылается InitialFileId: без него нельзя
            // создать проверку без загрузки новой версии, поэтому его не удаляем.
            if (source.Id == initialFileId)
                continue;

            try
            {
                var fileName = fileNameSelector(source.Data);
                if (fileName is null)
                    await fileRepository.DeleteFileAsync(bucket, source.Id);
                else
                    await fileRepository.DeleteFileAsync(bucket, source.Id, fileName);
                deletedCount++;
            }
            catch (Exception e)
            {
                logger.LogWarning(e,
                    "Failed to delete old version file of report '{reportId}' check '{checkId}' from S3 bucket '{bucket}'",
                    reportId, check.Id, bucket);
            }
        }

        if (deletedCount > 0 && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Deleted {count} old version file(s) of report '{reportId}' from S3 bucket '{bucket}'",
                deletedCount, reportId, bucket);
    }
}
