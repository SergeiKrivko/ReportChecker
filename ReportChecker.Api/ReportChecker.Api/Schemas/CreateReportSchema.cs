namespace ReportChecker.Api.Schemas;

public class CreateReportSchema
{
    public required string Name { get; init; }
    public required string Format { get; init; }
    public required string SourceProvider { get; init; }
    public string? Source { get; init; }
}