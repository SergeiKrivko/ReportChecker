using AiAgent.Models;
using ReportChecker.Models;

namespace AiAgent;

public interface IAiAgent: IAsyncDisposable
{
    /// <summary>Накопленный за время жизни клиента расход.</summary>
    public AiUsageSummary Usage { get; }

    public Task<IssueCreateAgent[]?> FindIssues(IssuesRequestAgent param, CancellationToken ct = default);
    public Task<CommentResponseAgent?> WriteComment(WriteCommentRequestAgent param, CancellationToken ct = default);
    public Task<CommentCreateAgent[]?> CheckIssues(IssuesRequestAgent param, CancellationToken ct = default);
    public Task<CommentCreateAgent[]?> ApplyInstruction(InstructionRequestAgent param, CancellationToken ct = default);
    public Task<IssueCreateAgent[]?> SearchInstruction(InstructionRequestAgent param, CancellationToken ct = default);
    public Task<IssueCreateAgent[]?> SearchAny(ChapterAgent[] param, CancellationToken ct = default);

    /// <summary>Сопоставляет оставшиеся найденные ошибки бенчмарка с известными.</summary>
    public Task<BenchmarkMatchAgent[]?> MatchBenchmarkIssues(BenchmarkMatchRequestAgent param,
        CancellationToken ct = default);

}