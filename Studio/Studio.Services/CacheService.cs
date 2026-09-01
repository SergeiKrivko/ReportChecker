using AvaluxUI.Utils;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.Services;

public class CacheService(ISettingsSection settings) : ICacheService
{
    public async Task SaveCacheAsync(string key, object data, CancellationToken ct = default)
    {
        var section = await settings.GetSection("cache");
        await section.Set(key, data);
    }

    public async Task SaveCacheAsync(Guid reportId, string key, object data, CancellationToken ct = default)
    {
        var reportSection = await settings.GetSection(reportId.ToString());
        var section = await reportSection.GetSection("cache");
        await section.Set(key, data);
    }

    public async Task<T?> LoadCacheAsync<T>(string key, CancellationToken ct = default)
    {
        var section = await settings.GetSection("cache");
        return await section.Get<T>(key);
    }

    public async Task<T?> LoadCacheAsync<T>(Guid reportId, string key, CancellationToken ct = default)
    {
        var reportSection = await settings.GetSection(reportId.ToString());
        var section = await reportSection.GetSection("cache");
        return await section.Get<T>(key);
    }
}