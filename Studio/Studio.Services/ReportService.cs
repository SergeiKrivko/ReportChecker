using System.Reactive.Subjects;
using AvaluxUI.Utils;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Check = ReportChecker.Shared.ApiClient.Check;
using Report = ReportChecker.Shared.Models.Report;
using ProgressStatus = ReportChecker.Shared.Models.ProgressStatus;

namespace ReportChecker.Studio.Services;

public class ReportService(
    IApiClient apiClient,
    ISettingsSection globalSettings) : IReportService
{
    private readonly BehaviorSubject<Report?> _currentReport = new(null);
    public IObservable<Report?> CurrentReport => _currentReport;
    private readonly BehaviorSubject<ProgressStatus> _status = new(ProgressStatus.Completed);
    public IObservable<ProgressStatus> Status => _status;

    public async Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsAllAsync(ct);
        // SelectReport(resp.FirstOrDefault()?.ToDomain());
        return resp.Select(e => e.ToDomain()).ToList();
    }

    public async Task<Report> GetReportById(Guid id, CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsAllAsync(ct);
        return resp.First(e => e.Id == id).ToDomain();
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}