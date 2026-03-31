using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface ICheckSourceRepository<TSource>
{
    public Task<CheckSource<TSource>?> GetByCheckIdAsync(Guid checkId, CancellationToken ct = default);
    public Task<CheckSource<TSource>?> GetByIdAsync(Guid sourceId, CancellationToken ct = default);
    public Task<Guid> CreateAsync(Guid? checkId, TSource data, CancellationToken ct = default);
    public Task<Guid> CreateAsync(Guid id, Guid? checkId, TSource data, CancellationToken ct = default);
    public Task<bool> AttachAsync(Guid sourceId, Guid checkId, CancellationToken ct = default);
}