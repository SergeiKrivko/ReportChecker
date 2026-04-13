using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface ICheckService
{
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, CheckSourceUnion source, string? name = null);
    public Task WriteCommentAsync(Guid reportId, Guid issueId);
    public Task<IEnumerable<Chapter>> GetChaptersAsync(Report report, Check check);
    public Task RunCheck(CheckContext context);
}