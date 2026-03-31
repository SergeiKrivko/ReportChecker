namespace ReportChecker.Models.Sources;

public class GitHubReportSource
{
    public required long RepositoryId { get; init; }
    public required string Branch { get; init; }
    public required string Path { get; init; }
}