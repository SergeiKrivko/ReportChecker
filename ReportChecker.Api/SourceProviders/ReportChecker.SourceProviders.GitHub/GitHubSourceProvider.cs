using System.IO.Compression;
using System.Text.Json;
using Octokit;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubSourceProvider(GithubService githubService) : ISourceProvider
{
    public string Key => "GitHub";

    public async Task<Stream> GetStreamAsync(string sourceJson)
    {
        var source = JsonSerializer.Deserialize<GitHubCommitSourceSchema>(sourceJson) ??
                     throw new InvalidOperationException();
        return await GetStreamAsync(source);
    }

    private async Task<Stream> GetStreamAsync(GitHubCommitSourceSchema source)
    {
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);
        var contents =
            await client.Repository.Content.GetAllContentsByRef(source.RepositoryId, source.FilePath, source.CommitId);
        var httpClient = new HttpClient();
        if (contents.Count == 1)
            return await httpClient.GetStreamAsync(contents[0].DownloadUrl);

        var stream = new MemoryStream();
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            foreach (var content in contents.Where(e => e.DownloadUrl != null))
            {
                await using var contentStream = await httpClient.GetStreamAsync(content.DownloadUrl);
                await using var entryStream = await zip.CreateEntry(content.Name, CompressionLevel.Optimal).OpenAsync();
                await contentStream.CopyToAsync(entryStream);
            }
        
        return new MemoryStream(stream.ToArray());
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

        return new SourceSchema(JsonSerializer.Serialize(result), await GetStreamAsync(result), branch.Commit.Label);
    }
}