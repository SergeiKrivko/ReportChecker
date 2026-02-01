namespace ReportChecker.Models;

public class Check
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public required Guid UserId { get; init; }
    public string? Name { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Source { get; init; }
    public ProgressStatus Status { get; init; }
}