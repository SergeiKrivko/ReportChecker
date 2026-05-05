using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ICheckRepository
{
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string? name = null, CancellationToken ct = default);
    public Task<Check?> GetCheckByIdAsync(Guid checkId, CancellationToken ct = default);
    public Task<Check?> GetPreviousCheckAsync(Check offset, CancellationToken ct = default);
    public Task<Check?> GetLatestCheckOfReportAsync(Guid reportId, CancellationToken ct = default);
    public Task<IEnumerable<Check>> GetAllChecksOfReportAsync(Guid reportId, CancellationToken ct = default);
    public Task SetCheckStatusAsync(Guid checkId, ProgressStatus status, CancellationToken ct = default);
    public Task<int> CountChecksAsync(Guid userId, DateTime startDate, CancellationToken ct = default);
}