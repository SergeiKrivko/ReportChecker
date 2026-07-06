using System.Reactive.Subjects;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Services.Converters;
using Report = ReportChecker.Shared.Models.Report;

namespace ReportChecker.Studio.Services;

public class ReportService(IApiClient apiClient) : IReportService
{
    private readonly BehaviorSubject<Report?> _currentReport = new(null);
    public IObservable<Report?> CurrentReport => _currentReport;

    public async Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsAllAsync(ct);
        var result = resp.Select(e => e.ToDomain()).ToList();
        SelectReport(result[0]);
        return result;
    }

    public async Task<Report> GetReportById(Guid id, CancellationToken ct = default)
    {
        var resp = await apiClient.ReportsGETAsync(id, ct);
        return resp.ToDomain();
    }

    public void SelectReport(Report report)
    {
        _currentReport.OnNext(report);
    }
}