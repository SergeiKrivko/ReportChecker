using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class UpdateReportDto
{
    public required string Name { get; init; }
    public Guid? LlmModelId { get; init; }
    public ImageProcessingMode ImageProcessingMode { get; init; } = ImageProcessingMode.Disable;
    public LlmReasoningEffort ReasoningLevel { get; init; } = LlmReasoningEffort.Default;
}