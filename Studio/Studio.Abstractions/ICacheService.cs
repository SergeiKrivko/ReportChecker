namespace ReportChecker.Studio.Abstractions;

public interface ICacheService
{
    public Task SaveCacheAsync(string key, object data, CancellationToken ct = default);
    public Task SaveCacheAsync(Guid reportId, string key, object data, CancellationToken ct = default);
    public Task<T?> LoadCacheAsync<T>(string key, CancellationToken ct = default);
    public Task<T?> LoadCacheAsync<T>(Guid reportId, string key, CancellationToken ct = default);
}