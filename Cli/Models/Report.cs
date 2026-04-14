namespace ReportChecker.Cli.Models;

public class Report
{
    public required Guid Id { get; init; }
    public string? Name { get; init; }
}