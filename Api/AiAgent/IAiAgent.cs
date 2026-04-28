using AiAgent.Models;

namespace AiAgent;

public interface IAiAgent: IAsyncDisposable
{
    public Task<IssueCreateAgent[]?> FindIssues(IssuesRequestAgent param);
    public Task<CommentResponseAgent?> WriteComment(WriteCommentRequestAgent param);
    public Task<CommentCreateAgent[]?> CheckIssues(IssuesRequestAgent param);
    public Task<CommentCreateAgent[]?> ApplyInstruction(InstructionRequestAgent param);
    public Task<IssueCreateAgent[]?> SearchInstruction(InstructionRequestAgent param);

}