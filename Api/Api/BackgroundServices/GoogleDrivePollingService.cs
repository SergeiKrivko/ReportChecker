using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using StackExchange.Redis;

namespace ReportChecker.Api.BackgroundServices;

/// <summary>
/// Периодически сверяет отчёты из Google Диска с Диском и создаёт проверку только
/// при изменении содержимого. Проверка уже идущего отчёта не дублируется.
/// </summary>
public class GoogleDrivePollingService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IConnectionMultiplexer redisConnection,
    ILogger<GoogleDrivePollingService> logger) : BackgroundService
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = int.TryParse(configuration["GoogleDrive:PollingIntervalMinutes"], out var configured)
            ? configured
            : 10;
        if (minutes <= 0)
        {
            logger.LogInformation("Google Drive polling is disabled (GoogleDrive:PollingIntervalMinutes <= 0)");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
        logger.LogInformation("Google Drive polling started with interval {interval}", interval);

        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
                await PollAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled error in Google Drive polling");
            }
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
        var syncService = scope.ServiceProvider.GetRequiredService<IGoogleDriveSyncService>();
        var redis = redisConnection.GetDatabase(3);

        var reports = await reportRepository.GetAllReportsOfSourceAsync("GoogleDrive");
        foreach (var report in reports)
        {
            ct.ThrowIfCancellationRequested();

            var lockKey = $"google-drive-sync-{report.Id}";
            if (!await redis.LockTakeAsync(lockKey, _instanceId, TimeSpan.FromMinutes(15)))
                continue;

            try
            {
                await syncService.SyncAsync(report.Id, false, ct);
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning("Google Drive is not connected for report '{reportId}': {message}", report.Id,
                    e.Message);
            }
            catch (NotFoundException e)
            {
                logger.LogWarning("Google Drive source of report '{reportId}' is missing: {message}", report.Id,
                    e.Message);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Failed to sync report '{reportId}' with Google Drive", report.Id);
            }
            finally
            {
                await redis.LockReleaseAsync(lockKey, _instanceId);
            }
        }
    }
}
