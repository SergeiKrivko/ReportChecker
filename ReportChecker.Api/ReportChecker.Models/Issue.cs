namespace ReportChecker.Models;

public class Issue
{
    public required Guid Id { get; init; }
    public required Guid CheckId { get; init; }
    public required string Title { get; init; }
    public IssueStatus Status { get; init; }
    public int Priority { get; init; } = 1;
    public required string Chapter { get; init; }

    public Comment[] Comments { get; init; } = [];
}