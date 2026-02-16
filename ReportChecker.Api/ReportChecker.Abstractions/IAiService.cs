using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAiService
{
    public Task FindIssuesAsync(Guid checkId, IEnumerable<Chapter> chapters, List<Chapter> existingChapters, List<Issue> existingIssues);
    public Task WriteComment(Guid issueId, List<Chapter> chapters);
}