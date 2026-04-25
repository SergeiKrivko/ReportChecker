namespace ReportChecker.Models;

public class LlmModel
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required string ModelKey { get; init; }
    public double InputCoefficient { get; init; }
    public double OutputCoefficient { get; init; }
    public bool IsDefault { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}