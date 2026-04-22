using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ILlmModelRepository
{
    public Task<IReadOnlyList<LlmModel>> GetAllModelsAsync(CancellationToken ct = default);
    public Task<LlmModel?> GetModelByIdAsync(Guid id, CancellationToken ct = default);
    public Task<LlmModel> GetDefaultModelAsync(CancellationToken ct = default);
    public Task<Guid> CreateModelAsync(string displayName, string modelKey, CancellationToken ct = default);
    public Task<bool> DeleteModelAsync(Guid id, CancellationToken ct = default);
}