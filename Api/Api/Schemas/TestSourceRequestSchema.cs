using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Schemas;

public class TestSourceRequestSchema
{
    public required string Provider { get; init; }
    public required ReportSourceUnion Source { get; init; }
}