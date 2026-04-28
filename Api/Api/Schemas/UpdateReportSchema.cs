using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class UpdateReportSchema
{
    public required string Name { get; init; }
    public Guid? LlmModelId { get; init; }
    public ImageProcessingMode ImageProcessingMode { get; init; } = ImageProcessingMode.Disable;
}