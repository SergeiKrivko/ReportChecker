namespace ReportChecker.Models.Sources;

public record ReportSourceUnion
{
    public GitHubReportSource? GitHub { get; init; }
    public FileReportSource? File { get; init; }
}