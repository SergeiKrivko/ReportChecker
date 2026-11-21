using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.GoogleDrive;

public class GoogleDriveSyncService(
    IReportRepository reportRepository,
    IReportSourceRepository<GoogleDriveReportSource> reportSourceRepository,
    ICheckRepository checkRepository,
    ICheckSourceRepository<GoogleDriveCheckSource> checkSourceRepository,
    IGoogleDriveTokenService tokenService,
    IGoogleDriveClient client,
    IGoogleDriveSnapshotService snapshotService,
    ICheckService checkService,
    ILogger<GoogleDriveSyncService> logger) : IGoogleDriveSyncService
{
    public async Task<GoogleDriveSyncResult> SyncAsync(Guid reportId, bool force, CancellationToken ct = default)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId, ct) ??
                     throw new NotFoundException("Report not found");
        if (!string.Equals(report.SourceProvider, "GoogleDrive", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("Отчёт создан не из Google Диска");

        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId, ct) ??
                           throw new NotFoundException("Report source not found");

        var latest = await checkRepository.GetLatestCheckOfReportAsync(reportId, true, ct);
        if (latest != null && IsActive(latest.Status))
        {
            logger.LogInformation("Google Drive sync of report '{reportId}' skipped: check '{checkId}' is {status}",
                reportId, latest.Id, latest.Status);
            return new GoogleDriveSyncResult { Created = false, Reason = "Проверка уже выполняется" };
        }

        var token = await tokenService.GetAccessTokenAsync(report.OwnerId, ct);
        var state = await client.GetStateAsync(token, reportSource.Data.FileId, ct);

        if (latest != null)
        {
            var latestSource = await checkSourceRepository.GetByCheckIdAsync(latest.Id, ct);
            if (!force && latestSource != null &&
                string.Equals(latestSource.Data.ContentHash, state.Hash, StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "Google Drive sync of report '{reportId}' skipped: content of '{fileId}' has not changed",
                    reportId, reportSource.Data.FileId);
                return new GoogleDriveSyncResult { Created = false, Reason = "Файл на Диске не изменился" };
            }
        }

        var snapshot = await snapshotService.CreateSnapshotAsync(report.OwnerId, reportSource.Data.FileId, state,
            ct: ct);
        var checkId = await checkService.CreateCheckAsync(reportId, report.OwnerId, new CheckSourceUnion
        {
            Id = snapshot.Id,
            GoogleDrive = snapshot.Data,
        }, state.Name);

        logger.LogInformation("Google Drive sync of report '{reportId}' created check '{checkId}' for '{fileId}'",
            reportId, checkId, state.FileId);

        return new GoogleDriveSyncResult { Created = true, CheckId = checkId };
    }

    private static bool IsActive(ProgressStatus status)
    {
        return status is ProgressStatus.Queued or ProgressStatus.InProgress or ProgressStatus.CancellationRequested;
    }
}
