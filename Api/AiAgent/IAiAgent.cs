using AiAgent.Models;

namespace AiAgent;

public interface IAiAgent: IAsyncDisposable
{
    public Task<IssueCreateAgent[]?> FindIssues(IssuesRequestAgent param, CancellationToken ct = default);
    public Task<CommentResponseAgent?> WriteComment(WriteCommentRequestAgent param, CancellationToken ct = default);
    public Task<CommentCreateAgent[]?> CheckIssues(IssuesRequestAgent param, CancellationToken ct = default);
    public Task<CommentCreateAgent[]?> ApplyInstruction(InstructionRequestAgent param, CancellationToken ct = default);
    public Task<IssueCreateAgent[]?> SearchInstruction(InstructionRequestAgent param, CancellationToken ct = default);

}