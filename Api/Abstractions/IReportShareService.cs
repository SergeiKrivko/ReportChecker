using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportShareService
{
    public Task<IReadOnlyList<ReportShareInfo>> GetSharesAsync(Guid reportId, Guid requesterId,
        CancellationToken ct = default);

    public Task<ReportShareInfo> ShareWithUserAsync(Guid reportId, Guid requesterId, string loginOrEmail,
        ReportAccessLevel accessLevel, CancellationToken ct = default);

    public Task<ReportShareInfo> UpdateShareAsync(Guid reportId, Guid requesterId, Guid userId,
        ReportAccessLevel accessLevel, CancellationToken ct = default);

    public Task RevokeShareAsync(Guid reportId, Guid requesterId, Guid userId, CancellationToken ct = default);

    public Task<ReportShareLink?> GetLinkAsync(Guid reportId, Guid requesterId, CancellationToken ct = default);
    public Task<ReportShareLink> CreateLinkAsync(Guid reportId, Guid requesterId, CancellationToken ct = default);
    public Task<ReportShareLink> RotateLinkAsync(Guid reportId, Guid requesterId, CancellationToken ct = default);
    public Task RevokeLinkAsync(Guid reportId, Guid requesterId, CancellationToken ct = default);
}
