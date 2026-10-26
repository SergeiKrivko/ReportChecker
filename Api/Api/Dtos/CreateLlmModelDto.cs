using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CreateLlmModelDto
{
    public required string DisplayName { get; init; }
    public required string ModelKey { get; init; }
    public double InputCoefficient { get; init; } = 1;
    public double OutputCoefficient { get; init; } = 1;
    public LlmReasoningEffort DefaultReasoningLevel { get; init; } = LlmReasoningEffort.Default;
    public bool IsRecommended { get; init; }
}