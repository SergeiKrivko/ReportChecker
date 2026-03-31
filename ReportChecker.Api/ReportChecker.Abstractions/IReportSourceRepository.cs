using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface IReportSourceRepository<TSource> where TSource : class
{
    public Task<ReportSource<TSource>?> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);
    public Task<ReportSource<TSource>?> GetByIdAsync(Guid sourceId, CancellationToken ct = default);
    public Task<IReadOnlyList<ReportSource<TSource>>> GetByExternalIdAsync<T>(T externalId, CancellationToken ct = default);
    public Task<Guid> CreateAsync(Guid reportId, TSource data, CancellationToken ct = default);
}