using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IIssueRepository
{
    public Task<IEnumerable<Issue>> GetAllIssuesOfCheckAsync(Guid checkId, CancellationToken ct = default);
    public Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId, CancellationToken ct = default);

    public Task<IEnumerable<Issue>> GetAllIssuesOfReportAsync(Guid reportId, Guid userId,
        CancellationToken ct = default);

    public Task<Issue?> GetIssueByIdAsync(Guid issueId, CancellationToken ct = default);
    public Task<Issue?> GetIssueByIdAsync(Guid issueId, Guid userId, CancellationToken ct = default);

    public Task<Guid> CreateIssueAsync(Guid checkId, string chapter, int line, string title, int? priority = 1,
        CancellationToken ct = default);

    public Task UpdateIssueLocationAsync(Guid issueId, Guid checkId, string chapter, int? line, CancellationToken ct = default);
}