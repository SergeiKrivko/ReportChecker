namespace ReportChecker.Api.Dtos;

public class ReportSourceUnionDto
{
    public GitHubReportSourceDto? GitHub { get; init; }
    public FileReportSourceDto? File { get; init; }
    public LocalReportSourceDto? Local { get; init; }
}