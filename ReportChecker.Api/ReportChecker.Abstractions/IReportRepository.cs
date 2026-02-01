using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportRepository
{
    public Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider, string? source);
    public Task<bool> DeleteReportAsync(Guid reportId);
    public Task<bool> RenameReportAsync(Guid reportId, string newName);
    public Task<Report?> GetReportByIdAsync(Guid reportId);
    public Task<IEnumerable<Report>> GetAllReportsOfUserAsync(Guid userId);
}