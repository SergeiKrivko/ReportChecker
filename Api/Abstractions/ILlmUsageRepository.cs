using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ILlmUsageRepository
{
    public Task<IReadOnlyList<LlmUsage>> GetAllUsagesForReportAsync(Guid reportId, CancellationToken ct = default);
    public Task<IReadOnlyList<LlmUsage>> GetAllUsagesOfModelAsync(Guid modelId, CancellationToken ct = default);
    public Task<Guid> CreateUsageAsync(LlmUsage usage, CancellationToken ct = default);
    public Task<int> GetTotalUsageAsync(Guid userId, DateTime startsAt, CancellationToken ct = default);

    public Task<IReadOnlyDictionary<Guid, int>> GetModelsUsageAsync(Guid userId, DateTime timeFrom, DateTime timeTo,
        CancellationToken ct = default);

    public Task<IReadOnlyDictionary<Guid, int>> GetReportsUsageAsync(Guid userId, DateTime timeFrom, DateTime timeTo,
        CancellationToken ct = default);

    public Task<IReadOnlyList<LlmUsageGroup>> GetUsageStatisticsAsync(Guid userId, DateTime timeFrom, DateTime timeTo,
        CancellationToken ct = default);

    public Task<IReadOnlyList<LlmUsageInterval>> GetTimeUsageAsync(Guid userId, DateTime timeFrom, DateTime timeTo,
        Guid? modelId = null, Guid? reportId = null,
        CancellationToken ct = default);
}