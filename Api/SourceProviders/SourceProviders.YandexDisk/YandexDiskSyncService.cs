using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.YandexDisk;

public class YandexDiskSyncService(
    IReportRepository reportRepository,
    IReportSourceRepository<YandexDiskReportSource> reportSourceRepository,
    ICheckRepository checkRepository,
    ICheckSourceRepository<YandexDiskCheckSource> checkSourceRepository,
    IYandexDiskTokenService tokenService,
    IYandexDiskClient client,
    IYandexDiskSnapshotService snapshotService,
    ICheckService checkService,
    ILogger<YandexDiskSyncService> logger) : IYandexDiskSyncService
{
    public async Task<YandexDiskSyncResult> SyncAsync(Guid reportId, bool force, CancellationToken ct = default)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId, ct) ??
                     throw new NotFoundException("Report not found");
        if (!string.Equals(report.SourceProvider, "YandexDisk", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Отчёт создан не из Яндекс.Диска");

        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId, ct) ??
                           throw new NotFoundException("Report source not found");

        var latest = await checkRepository.GetLatestCheckOfReportAsync(reportId, true, ct);
        if (latest != null && IsActive(latest.Status))
        {
            logger.LogInformation("Yandex.Disk sync of report '{reportId}' skipped: check '{checkId}' is {status}",
                reportId, latest.Id, latest.Status);
            return new YandexDiskSyncResult { Created = false, Reason = "Проверка уже выполняется" };
        }

        var token = await tokenService.GetAccessTokenAsync(report.OwnerId, ct);
        var state = await client.GetStateAsync(token, reportSource.Data.Path, ct);

        if (latest != null)
        {
            var latestSource = await checkSourceRepository.GetByCheckIdAsync(latest.Id, ct);
            if (!force && latestSource != null &&
                string.Equals(latestSource.Data.ContentHash, state.Hash, StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "Yandex.Disk sync of report '{reportId}' skipped: content of '{path}' has not changed",
                    reportId, reportSource.Data.Path);
                return new YandexDiskSyncResult { Created = false, Reason = "Файл на Диске не изменился" };
            }
        }

        var snapshot = await snapshotService.CreateSnapshotAsync(report.OwnerId, reportSource.Data.Path, state,
            ct: ct);
        var checkId = await checkService.CreateCheckAsync(reportId, report.OwnerId, new CheckSourceUnion
        {
            Id = snapshot.Id,
            YandexDisk = snapshot.Data,
        }, state.Name);

        logger.LogInformation("Yandex.Disk sync of report '{reportId}' created check '{checkId}' for '{path}'",
            reportId, checkId, state.Path);

        return new YandexDiskSyncResult { Created = true, CheckId = checkId };
    }

    private static bool IsActive(ProgressStatus status)
    {
        return status is ProgressStatus.Queued or ProgressStatus.InProgress or ProgressStatus.CancellationRequested;
    }
}
