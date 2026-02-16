using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ICheckRepository
{
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string? source = null, string? name = null);
    public Task<Check?> GetCheckByIdAsync(Guid checkId);
    public Task<Check?> GetPreviousCheckAsync(Check offset);
    public Task<Check?> GetLatestCheckOfReportAsync(Guid reportId);
    public Task<IEnumerable<Check>> GetAllChecksOfReportAsync(Guid reportId);
    public Task SetCheckStatusAsync(Guid checkId, ProgressStatus status);
}