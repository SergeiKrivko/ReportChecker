namespace ReportChecker.Api.Schemas;

public class CreateLlmModelSchema
{
    public required string DisplayName { get; init; }
    public required string ModelKey { get; init; }
}