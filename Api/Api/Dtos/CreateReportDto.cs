using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CreateReportDto
{
    public required string Name { get; init; }
    public required string Format { get; init; }
    public required string SourceProvider { get; init; }
    public required ReportSourceUnionDto Source { get; init; }
    public Guid? LlmModelId { get; init; }
    public ImageProcessingMode ImageProcessingMode { get; init; } = ImageProcessingMode.Disable;
    public LlmReasoningEffort ReasoningLevel { get; init; } = LlmReasoningEffort.Default;
}