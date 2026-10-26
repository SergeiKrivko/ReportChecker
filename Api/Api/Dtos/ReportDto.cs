using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class ReportDto
{
    public required Guid Id { get; init; }
    public required Guid OwnerId { get; init; }
    public string? Name { get; init; }
    public required string SourceProvider { get; init; }
    public required string Format { get; init; }
    public Guid? LlmModelId { get; init; }
    public ImageProcessingMode ImageProcessingMode { get; init; }
    public LlmReasoningEffort ReasoningLevel { get; init; } = LlmReasoningEffort.Default;
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public ReportSourceUnionDto? Source { get; init; }
    public Dictionary<int, int> IssueCount { get; init; } = [];
    public DateTime? UpdatedAt { get; init; }
}