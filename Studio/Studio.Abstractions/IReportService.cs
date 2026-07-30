using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IReportService
{
    public IObservable<Report?> CurrentReport { get; }
    public IObservable<ProgressStatus> Status { get; }
    public Task<IReadOnlyList<Report>> GetAllReports(CancellationToken ct = default);
    public Task<Report> GetReportById(Guid id, CancellationToken ct = default);
    public void SelectReport(Report? report);
    public Task SelectReport(Guid reportId);
    public Task SelectReport(Guid? reportId);
    public Task<Guid> CreateAsync(SourcePack pack, string name, CancellationToken ct = default);
    public Task<Guid> CheckAsync(SourcePack pack, CancellationToken ct = default);
}