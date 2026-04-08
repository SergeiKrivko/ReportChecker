using System.Security.Cryptography;
using System.Text;
using Octokit;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubArchive(GitHubClient client, long repositoryId, string commitRef, string branch, string? rootName)
    : IFileArchive
{
    public string? Name => rootName;

    public string? EntryFilePath => rootName;
    private readonly HttpClient _httpClient = new();

    private readonly string? _basePath = Path.GetDirectoryName(rootName);

    public async Task<Stream?> ReadAsync(string name)
    {
        name = $"{_basePath}/{name.TrimStart('/')}".TrimStart('/');
        return await _OpenAsync(name);
    }

    public async Task<Stream?> ReadAsync()
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

    public async Task WriteAsync(string name, Stream content, CancellationToken ct)
    {
        name = $"{_basePath}/{name.TrimStart('/')}".TrimStart('/');
        await _WriteAsync(name, content, ct);
    }

    public async Task WriteAsync(Stream content, CancellationToken ct)
    {
        if (rootName == null)
            throw new Exception("No root file");
        await _WriteAsync(rootName, content, ct);
    }

    private async Task<string> GetShaAsync(string name, CancellationToken ct)
    {
        var contents = await client.Repository.Content.GetAllContentsByRef(repositoryId, name, commitRef);
        if (contents == null)
            throw new Exception("File not found");
        if (contents.Count != 1)
            throw new Exception("Not a file");
        return contents[0].Sha;
    }

    private async Task _WriteAsync(string name, Stream content, CancellationToken ct)
    {
        var sha = await GetShaAsync(name, ct);

        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, ct);
        var bytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(bytes);
        Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
        await client.Repository.Content.UpdateFile(repositoryId, name,
            new UpdateFileRequest($"Fix issue in file '{name}' by ReportChecker", base64, sha, branch,
                convertContentToBase64: false));
    }
}