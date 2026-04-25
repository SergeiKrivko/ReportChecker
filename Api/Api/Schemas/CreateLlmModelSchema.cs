namespace ReportChecker.Api.Schemas;

public class CreateLlmModelSchema
{
    public required string DisplayName { get; init; }
    public required string ModelKey { get; init; }
    public double InputCoefficient { get; init; } = 1;
    public double OutputCoefficient { get; init; } = 1;
}