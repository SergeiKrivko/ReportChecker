using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class LlmUsageEntity
{
    public required Guid Id { get; init; }
    public required Guid ModelId { get; init; }
    public required Guid ReportId { get; init; }
    public required LlmUsageType Type { get; init; }
    public required DateTime FinishedAt { get; init; }

    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }

    public virtual LlmModelEntity Model { get; init; } = null!;
    public virtual ReportEntity Report { get; init; } = null!;
}