using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiService
{
    public Task FindIssuesAsync(CheckContext context);

    public Task WriteComment(CheckContext context, Issue issue);

    public Task ProcessInstructionApplyAsync(Guid taskId, CheckContext context, string instruction);

    public Task ProcessInstructionSearchAsync(Guid taskId, CheckContext context, string instruction);
}