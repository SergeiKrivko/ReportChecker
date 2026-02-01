namespace ReportChecker.Abstractions;

public interface ICheckService
{
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string source, string? name = null);
    public Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, SourceSchema source);
    public Task WriteCommentAsync(Guid checkId, Guid issueId);

    public void RunCheck(Guid checkId, Stream source);
    public void RunCheck(Guid checkId);
}