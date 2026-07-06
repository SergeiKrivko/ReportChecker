using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IReportService
{
    public IObservable<Report?> CurrentReport { get; }
    public Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default);
    public Task<Report> GetReportById(Guid id, CancellationToken ct = default);
    public void SelectReport(Report report);
}