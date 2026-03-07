namespace ReportChecker.Api.Schemas;

public class TestSourceRequestSchema
{
    public required string Provider { get; init; }
    public required string Source { get; init; }
}