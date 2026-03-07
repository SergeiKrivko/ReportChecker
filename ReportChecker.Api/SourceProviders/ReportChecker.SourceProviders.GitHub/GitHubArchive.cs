using Octokit;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubArchive(IReadOnlyList<RepositoryContent> contents, string? rootName) : IFileArchive
{
    public string? Name => rootName;

    private readonly HttpClient _httpClient = new HttpClient();

    public async Task<Stream?> OpenAsync(string name)
    {
        var content = contents.FirstOrDefault(x => x.Name == name);
        if (content == null)
            return null;
        return await _httpClient.GetStreamAsync(content.DownloadUrl);
    }

    public async Task<Stream?> OpenAsync()
    {
        if (contents.Count > 1)
            return null;
        return await _httpClient.GetStreamAsync(contents[0].DownloadUrl);
    }
}