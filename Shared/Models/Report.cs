namespace ReportChecker.Shared.Models;

public class Report
{
    public required Guid Id { get; init; }
    public string? Name { get; init; }
    public Dictionary<string, int> IssueCount { get; init; } = [];
}