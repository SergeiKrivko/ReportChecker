using Octokit;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubArchive(GitHubClient client, long repositoryId, string commitRef, string? rootName)
    : IFileArchive
{
    public string? Name => rootName;

    public string? EntryFilePath => rootName;
    private readonly HttpClient _httpClient = new();

    private readonly string? _basePath = Path.GetDirectoryName(rootName);

    public async Task<Stream?> OpenAsync(string name)
    {
        name = $"{_basePath}/{name.TrimStart('/')}".TrimStart('/');
        return await _OpenAsync(name);
    }

    public async Task<Stream?> OpenAsync()
    {
        if (rootName == null)
            return null;
        return await _OpenAsync(rootName);
    }

    private async Task<Stream?> _OpenAsync(string name)
    {
        var contents = await client.Repository.Content.GetAllContentsByRef(repositoryId, name, commitRef);
        if (contents == null)
            return null;
        if (contents.Count != 1)
            return null;
        return await _httpClient.GetStreamAsync(contents[0].DownloadUrl);
    }
}