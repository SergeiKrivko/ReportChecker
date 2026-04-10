namespace ReportChecker.Models.Sources;

public class CheckSourceUnion
{
    public Guid? Id { get; init; }
    public GitHubCheckSource? GitHub { get; init; }
    public FileCheckSource? File { get; init; }
    public LocalCheckSource? Local { get; init; }
}