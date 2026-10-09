using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class BenchmarkRunEntity
{
    public required Guid Id { get; init; }
    [MaxLength(100)] public required string CaseId { get; init; }
    [MaxLength(200)] public required string CaseName { get; init; }
    public required Guid ModelId { get; init; }
    public LlmReasoningEffort ReasoningEffort { get; init; }
    public ProgressStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    [MaxLength(2000)] public string? FailureReason { get; init; }

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

    public virtual LlmModelEntity Model { get; init; } = null!;
    public virtual ICollection<BenchmarkResultEntity> Results { get; init; } = null!;
}
