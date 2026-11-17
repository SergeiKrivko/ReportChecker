using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IReportAccessService
{
    public Task<ReportAccess> RequireAccessAsync(Guid reportId, Guid userId, ReportAccessLevel required,
        CancellationToken ct = default);

    public Task<ReportAccess> RequireOwnerAsync(Guid reportId, Guid userId, CancellationToken ct = default);

    public Task<Report> RequireSharedReportAsync(string token, CancellationToken ct = default);
}
