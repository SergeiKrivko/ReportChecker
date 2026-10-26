using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class BenchmarkRunDto
{
    public required Guid Id { get; init; }

    public required string CaseId { get; init; }

    public required string CaseName { get; init; }

    public required Guid ModelId { get; init; }

    public string? ModelDisplayName { get; init; }

    public LlmReasoningEffort ReasoningEffort { get; init; }

    public ProgressStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public string? FailureReason { get; init; }

    public int ExpectedCount { get; init; }

    public int FoundCount { get; init; }

    public int MatchedCount { get; init; }

    public int TitleMatchCount { get; init; }

    public int PriorityMatchCount { get; init; }

    public int FixCheckedCount { get; init; }

    public int FixMatchCount { get; init; }

    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalCost { get; init; }

    public BenchmarkResultDto[] Results { get; init; } = [];

    public long? DurationMs => StartedAt != null && FinishedAt != null
        ? (long)(FinishedAt.Value - StartedAt.Value).TotalMilliseconds
        : null;
}