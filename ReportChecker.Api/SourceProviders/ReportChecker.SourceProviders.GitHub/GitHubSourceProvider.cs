using System.Text.Json;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubSourceProvider(GithubService githubService) : ISourceProvider
{
    public string Key => "GitHub";

    public async Task<IFileArchive> OpenAsync(string sourceJson)
    {
        var source = JsonSerializer.Deserialize<GitHubCommitSourceSchema>(sourceJson) ??
                     throw new InvalidOperationException();
        return await OpenAsync(source);
    }

    private async Task<IFileArchive> OpenAsync(GitHubCommitSourceSchema source)
    {
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);
        var contents =
            await client.Repository.Content.GetAllContentsByRef(source.RepositoryId, source.FilePath, source.CommitId);
        return new GitHubArchive(contents, source.FilePath);
    }

    public async Task<SourceSchema> FindSourceAsync(string sourceJson)
    {
        var source = JsonSerializer.Deserialize<GitHubSourceSchema>(sourceJson) ??
                     throw new InvalidOperationException();
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);
        var branch = await client.Repository.Branch.Get(source.RepositoryId, source.BranchName);

        var result = new GitHubCommitSourceSchema
        {
            RepositoryId = source.RepositoryId,
            BranchName = branch.Name,
            FilePath = source.FilePath,
            CommitId = branch.Commit.Sha
        };

        return new SourceSchema(JsonSerializer.Serialize(result), await OpenAsync(result), branch.Commit.Label);
    }
}