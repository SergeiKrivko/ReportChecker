using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Schemas;

public class CreateReportSchema
{
    public required string Name { get; init; }
    public required string Format { get; init; }
    public required string SourceProvider { get; init; }
    public required ReportSourceUnion Source { get; init; }
    public Guid? LlmModelId { get; init; }
    public ImageProcessingMode ImageProcessingMode { get; init; } = ImageProcessingMode.Disable;
}