using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ICommentRepository
{
    public Task<IEnumerable<Comment>> GetAllCommentsOfIssueAsync(Guid issueId);
    public Task<Comment?> GetCommentByIdAsync(Guid commentId);

    public Task<Guid> CreateCommentAsync(Guid issueId, Guid userId, string? content, IssueStatus? status,
        ProgressStatus? progressStatus = null);

    public Task UpdateCommentAsync(Guid commentId, string comment);
    public Task DeleteCommentAsync(Guid commentId);
    public Task SetProgressStatusAsync(Guid commentId, ProgressStatus status);
}