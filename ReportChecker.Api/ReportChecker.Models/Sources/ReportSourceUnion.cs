namespace ReportChecker.Models.Sources;

public record ReportSourceUnion
{
    public GitHubReportSource? GitHub { get; init; }
    public FileReportSource? File { get; init; }
    public LocalReportSource? Local { get; init; }
}