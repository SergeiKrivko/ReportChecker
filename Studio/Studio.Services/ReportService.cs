using System.Reactive.Linq;
using System.Reactive.Subjects;
using AvaluxUI.Utils;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Converters;
using Check = ReportChecker.Shared.ApiClient.Check;
using Report = ReportChecker.Shared.Models.Report;
using ProgressStatus = ReportChecker.Shared.Models.ProgressStatus;

namespace ReportChecker.Studio.Services;

public class ReportService(
    IApiClient apiClient,
    ISettingsSection globalSettings,
    IAlertService alertService,
    ICacheService cacheService) : IReportService
{
    private readonly BehaviorSubject<Report?> _currentReport = new(null);
    public IObservable<Report?> CurrentReport => _currentReport;
    private readonly BehaviorSubject<ProgressStatus> _status = new(ProgressStatus.Completed);
    public IObservable<ProgressStatus> Status => _status;

    public async Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default)
    {
        try
        {
            var resp = await apiClient.ReportsAllAsync(ct);
            var res = resp.Select(e => e.ToDomain()).ToList();
            await cacheService.SaveCacheAsync("allReports", res, ct);
            return res;
        }
        catch (HttpRequestException e) when (e.HttpRequestError == HttpRequestError.ConnectionError ||
                                             e.HttpRequestError == HttpRequestError.NameResolutionError)
        {
            var cache = await cacheService.LoadCacheAsync<List<Report>>("allReports", ct);
            return cache ?? throw new Exception($"Http request exception: {e.Message}", e);
        }
    }

    public async Task<Report> GetReportById(Guid id, CancellationToken ct = default)
    {
        try
        {
            var resp = await apiClient.ReportsGETAsync(id, ct);
            var res = resp.ToDomain();
            await cacheService.SaveCacheAsync(id, "report", res, ct);
            return res;
        }
        catch (HttpRequestException e) when (e.HttpRequestError == HttpRequestError.ConnectionError ||
                                             e.HttpRequestError == HttpRequestError.NameResolutionError)
        {
            var cache = await cacheService.LoadCacheAsync<Report>(id, "report", ct);
            return cache ?? throw new Exception($"Http request exception: {e.Message}", e);
        }
    }

    public void SelectReport(Report? report)
    {
        _currentReport.OnNext(report);
    }

    public async Task SelectReport(Guid reportId)
    {
        SelectReport(await GetReportById(reportId));
    }

    public async Task SelectReport(Guid? reportId)
    {
        if (reportId == null)
            _currentReport.OnNext(null);
        else
            await SelectReport(reportId.Value);
    }

    public async Task<Guid> CreateAsync(SourcePack pack, string name, CancellationToken ct = default)
    {
        var file = await apiClient.FilesPOSTAsync(FileBucketDto.Local,
            new FileParameter(pack.Stream, pack.FileName), ct);
        var clientId = await GetClientIdAsync();
        var reportId = await apiClient.ReportsPOSTAsync(new CreateReportSchema
        {
            Format = pack.Format,
            Name = name,
            SourceProvider = "Local",
            Source = new ReportSourceUnion
            {
                Local = new LocalReportSource
                {
                    ClientId = clientId,
                    ClientMachineName = Environment.MachineName,
                    InitialFileId = file.Id,
                    EntryFilePath = pack.EntryFilePath,
                }
            },
            ImageProcessingMode = ImageProcessingMode.Disable,
            LlmModelId = null,
        }, ct);
        await StartPolling(reportId);
        return reportId;
    }

    public async Task<Guid> CheckAsync(SourcePack pack, CancellationToken ct = default)
    {
        var file = await apiClient.FilesPOSTAsync(FileBucketDto.Local,
            new FileParameter(pack.Stream, pack.FileName), ct);
        var report = await CurrentReport.FirstAsync() ?? throw new Exception("Report not selected");
        var checkId = await apiClient.ChecksPOSTAsync(report.Id, new CreateCheckSchema
        {
            Source = new CheckSourceUnion
            {
                Id = file.Id
            },
        }, ct);
        await StartPolling(report.Id);
        return checkId;
    }

    private const string SettingsClientIdKey = "clientId";

    private async Task<Guid> GetClientIdAsync()
    {
        var id = await globalSettings.Get<Guid>(SettingsClientIdKey);
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
            await globalSettings.Set(SettingsClientIdKey, id);
        }

        return id;
    }

    private CancellationTokenSource? _pollingCtSource;

    private async Task StartPolling(Guid reportId)
    {
        if (_pollingCtSource != null)
            await _pollingCtSource.CancelAsync();
        _pollingCtSource = new CancellationTokenSource();
        RunPolling(reportId, _pollingCtSource.Token);
        _pollingCtSource = null;
    }

    private async void RunPolling(Guid reportId, CancellationToken ct)
    {
        try
        {
            Check? latestCheck;
            do
            {
                latestCheck = await apiClient.LatestAsync(reportId, ct);
                _status.OnNext(latestCheck.Status?.ToDomain() ?? ProgressStatus.Failed);
                await Task.Delay(5000, ct);
            } while (latestCheck.Status == Shared.ApiClient.ProgressStatus.InProgress);

            switch (latestCheck.Status)
            {
                case Shared.ApiClient.ProgressStatus.Completed:
                    alertService.SendAlert(AlertType.Success, "Поиск ошибок успешно завершен");
                    break;
                case Shared.ApiClient.ProgressStatus.Failed:
                    alertService.SendAlert(AlertType.Error, "Ошибка при поиске ошибок");
                    break;
            }
        }
        catch (Exception e)
        {
            alertService.SendAlert(AlertType.Warning, $"Не удалось получить статус проверки: {e.Message}");
        }
    }
}