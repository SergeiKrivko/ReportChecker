using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Models;

public class Project
{
    public required Guid Id { get; init; }
    public string? Name { get; set; }
    public required string Path { get; init; }
    public required string Format { get; init; }
    public Report? Report { get; init; }
}