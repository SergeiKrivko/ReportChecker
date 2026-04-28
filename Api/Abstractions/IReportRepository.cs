using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportRepository
{
    public Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        Guid? llmModelId, ImageProcessingMode imageProcessingMode);

    public Task<bool> DeleteReportAsync(Guid reportId);

    public Task<bool> UpdateReportAsync(Guid reportId, string newName, Guid? llmModelId,
        ImageProcessingMode imageProcessingMode);

    public Task<Report?> GetReportByIdAsync(Guid reportId);
    public Task<IEnumerable<Report>> GetAllReportsOfUserAsync(Guid userId);
    public Task<IEnumerable<Report>> GetAllReportsOfSourceAsync(string sourceProvider);
    public Task<int> CountReportsAsync(Guid userId);
}