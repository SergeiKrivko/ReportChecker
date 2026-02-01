using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IIssueRepository
{
    public Task<IEnumerable<Issue>> GetAllIssuesOfCheckAsync(Guid checkId);
    public Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId);
    public Task<Issue?> GetIssueByIdAsync(Guid issueId);
    public Task<Guid> CreateIssueAsync(Guid checkId, string chapter, string title, int? priority = 1);
}