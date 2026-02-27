using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubSourceSchema
{
    public required long RepositoryId { get; init; }
    public string? BranchName { get; init; }
    public required string FilePath { get; init; }
}

public class GitHubCommitSourceSchema : GitHubSourceSchema
{
    public required string CommitId { get; init; }
}