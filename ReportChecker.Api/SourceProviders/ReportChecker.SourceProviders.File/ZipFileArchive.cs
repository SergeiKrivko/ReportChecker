using System.IO.Compression;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.File;

public class ZipFileArchive(ZipArchive archive, string zipName) : IFileArchive
{
    public string? Name => zipName;

    public async Task<Stream?> OpenAsync(string name)
    {
        var entry = archive.GetEntry(name);
        if (entry == null)
            return null;
        return await entry.OpenAsync();
    }

    public Task<Stream?> OpenAsync()
    {
        return Task.FromResult<Stream?>(null);
    }
}