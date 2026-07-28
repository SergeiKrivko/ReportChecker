using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ICommentsService
{
    public IObservable<IReadOnlyList<Comment>> AllComments { get; }
    public IObservable<object?> Load();
    public Task CreateComment(string content, CancellationToken ct = default);
    public Task CreateComment(IssueStatus status, CancellationToken ct = default);
    public Task SetPatchStatusAsync(Guid commentId, PatchStatus status, CancellationToken ct = default);
}