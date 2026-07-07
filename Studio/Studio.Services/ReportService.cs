using System.Reactive.Subjects;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Report = ReportChecker.Shared.Models.Report;
using ProgressStatus = ReportChecker.Shared.Models.ProgressStatus;

namespace ReportChecker.Studio.Services;

public class ReportService(IApiClient apiClient) : IReportService
{
    private readonly BehaviorSubject<Report?> _currentReport = new(null);
    public IObservable<Report?> CurrentReport => _currentReport;
    private readonly BehaviorSubject<ProgressStatus> _status = new(ProgressStatus.Completed);
    public IObservable<ProgressStatus> Status => _status;

    public async Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsAllAsync(ct);
        return resp.Select(e => e.ToDomain()).ToList();
    }

    public async Task<Report> GetReportById(Guid id, CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsGETAsync(id, ct);
        return resp.ToDomain();
    }

    public void SelectReport(Report? report)
    {
        _currentReport.OnNext(report);
    }

    public async Task SelectReport(Guid reportId)
    {
        var resp = await apiClient.ReportsGETAsync(reportId);
        SelectReport(resp.ToDomain());
    }

    public async Task SelectReport(Guid? reportId)
    {
        if (reportId == null)
            _currentReport.OnNext(null);
        else
            await SelectReport(reportId.Value);
    }
}