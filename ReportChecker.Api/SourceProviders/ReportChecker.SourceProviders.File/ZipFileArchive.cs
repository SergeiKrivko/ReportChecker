using System.IO.Compression;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.File;

public class ZipFileArchive(ZipArchive archive, string zipName, string? entryFile = null) : IFileArchive
{
    public string? Name => zipName;
    public string? EntryFilePath => entryFile;

    public async Task<Stream?> OpenAsync(string name)
    {
        name = name.TrimStart('/');
        var entry = archive.GetEntry(name);
        if (entry == null)
            return null;
        return await entry.OpenAsync();
    }

    public async Task<Stream?> OpenAsync() => entryFile == null ? null : await OpenAsync(entryFile);
}