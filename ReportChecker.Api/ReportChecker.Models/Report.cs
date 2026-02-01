namespace ReportChecker.Models;

public class Report
{
    public required Guid Id { get; init; }
    public required Guid OwnerId { get; init; }
    public string? Name { get; init; }
    public required string SourceProvider { get; init; }
    public required string Format { get; init; }
    public string? Source { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}