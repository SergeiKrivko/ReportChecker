namespace ReportChecker.Api.Dtos;

public class GitHubReportSourceDto
{
    public required long RepositoryId { get; init; }
    public required string Branch { get; init; }
    public required string Path { get; init; }
}