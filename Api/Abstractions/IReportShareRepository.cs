using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportShareRepository
{
    public Task<IReadOnlyList<ReportShare>> GetSharesAsync(Guid reportId, CancellationToken ct = default);
    public Task<ReportShare?> GetShareAsync(Guid reportId, Guid userId, CancellationToken ct = default);

    public Task<ReportShare> CreateShareAsync(Guid reportId, Guid userId, ReportAccessLevel accessLevel,
        CancellationToken ct = default);

    public Task<bool> UpdateShareAsync(Guid reportId, Guid userId, ReportAccessLevel accessLevel,
        CancellationToken ct = default);

    public Task<bool> DeleteShareAsync(Guid reportId, Guid userId, CancellationToken ct = default);

    public Task<ReportShareLink?> GetLinkAsync(Guid reportId, CancellationToken ct = default);
    public Task<ReportShareLink> CreateOrGetLinkAsync(Guid reportId, string token, CancellationToken ct = default);
    public Task<ReportShareLink> ReplaceLinkAsync(Guid reportId, string token, CancellationToken ct = default);
    public Task<bool> DeleteLinkAsync(Guid reportId, CancellationToken ct = default);
}
