using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ICheckService
{
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string source, string? name = null);
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, SourceSchema source);
    public Task WriteCommentAsync(Guid checkId, Guid issueId);
    public Task<IEnumerable<Chapter>> GetChaptersAsync(Report report, Check check);

    public void RunCheck(Guid checkId, IFileArchive source);
    public void RunCheck(Guid checkId);
}