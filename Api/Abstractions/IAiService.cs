using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiService
{
    public Task FindIssuesAsync(CheckContext context, CancellationToken ct = default);

    public Task WriteComment(CheckContext context, Issue issue, CancellationToken ct = default);

    public Task ProcessInstructionApplyAsync(Guid taskId, CheckContext context, string instruction,
        CancellationToken ct = default);

    public Task ProcessInstructionSearchAsync(Guid taskId, CheckContext context, string instruction,
        CancellationToken ct = default);

    public Task ProcessSearchAnyAsync(Guid taskId, CheckContext context, CancellationToken ct = default);
}