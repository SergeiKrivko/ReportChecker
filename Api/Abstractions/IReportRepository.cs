using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportRepository
{
    public Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProvider,
        Guid? llmModelId, ImageProcessingMode imageProcessingMode,
        LlmReasoningEffort reasoningLevel = LlmReasoningEffort.Default);

    public Task<bool> DeleteReportAsync(Guid reportId);

    public Task<bool> UpdateReportAsync(Guid reportId, string newName, Guid? llmModelId,
        ImageProcessingMode imageProcessingMode, LlmReasoningEffort reasoningLevel = LlmReasoningEffort.Default);

    public Task<Report?> GetReportByIdAsync(Guid reportId, CancellationToken ct = default);
    public Task<ReportAccess?> GetReportAccessAsync(Guid reportId, Guid userId, CancellationToken ct = default);
    public Task<Report?> GetReportByShareTokenAsync(string token, CancellationToken ct = default);
    public Task<Report?> GetReportDetailsAsync(Guid reportId, CancellationToken ct = default);
    public Task<IEnumerable<ReportAccess>> GetAllReportsOfUserAsync(Guid userId, CancellationToken ct = default);
    public Task<IEnumerable<Report>> GetAllReportsOfSourceAsync(string sourceProvider);
    public Task<int> CountReportsAsync(Guid userId);
}