namespace ReportChecker.Api.Dtos;

public class CheckSourceUnionDto
{
    public Guid? Id { get; init; }
    public GitHubCheckSourceDto? GitHub { get; init; }
    public FileCheckSourceDto? File { get; init; }
    public LocalCheckSourceDto? Local { get; init; }
}