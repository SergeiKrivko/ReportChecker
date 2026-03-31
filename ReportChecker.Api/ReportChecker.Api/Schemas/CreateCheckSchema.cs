using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Schemas;

public class CreateCheckSchema
{
    public CheckSourceUnion? Source { get; init; }
    public string? Name { get; init; }
}