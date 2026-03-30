using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiService
{
    public Task FindIssuesAsync(Guid reportId, Guid checkId, ICollection<Chapter> chapters,
        ICollection<Chapter> existingChapters, ICollection<Issue> existingIssues);

    public Task WriteComment(Report report, Guid issueId, List<Chapter> chapters);

    public Task ProcessInstructionApplyAsync(Guid taskId, Guid reportId, Guid checkId, List<Chapter> chapters,
        string instruction);

    public Task ProcessInstructionSearchAsync(Guid taskId, Guid reportId, Guid checkId, List<Chapter> chapters,
        string instruction);
}