using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ILlmUsageRepository
{
    public Task<IReadOnlyList<LlmUsage>> GetAllUsagesForReportAsync(Guid reportId, CancellationToken ct = default);
    public Task<IReadOnlyList<LlmUsage>> GetAllUsagesOfModelAsync(Guid modelId, CancellationToken ct = default);
    public Task<Guid> CreateUsageAsync(LlmUsage usage, CancellationToken ct = default);
    public Task<int> GetTotalUsageAsync(Guid userId, DateTime startsAt, CancellationToken ct = default);
}