using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Abstractions;

public interface IYandexDiskAccountRepository
{
    public Task<YandexDiskAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    public Task SaveAsync(YandexDiskAccount account, CancellationToken ct = default);
    public Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
