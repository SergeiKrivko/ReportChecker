using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportService
{
    public Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProviderKey,
        string source);

    public Task<Guid> CreateCheckAsync(Report report);
}