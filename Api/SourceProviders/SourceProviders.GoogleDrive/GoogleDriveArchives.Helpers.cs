using System.IO.Compression;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.GoogleDrive;

/// <summary>Определение файла входа по списку файлов архива/папки.</summary>
public static class GoogleDriveEntryDetector
{
    public static string? Detect(IEnumerable<string> paths)
    {
        var files = paths.Where(e => !e.EndsWith('/')).ToList();
        var tex = files.Where(e => e.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)).ToList();
        if (tex.Count == 1)
            return tex[0];

        var main = tex.FirstOrDefault(e => string.Equals(GetName(e), "main.tex", StringComparison.OrdinalIgnoreCase));
        if (main != null)
            return main;

        var supported = files
            .Where(e => e.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                        e.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                        e.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return supported.Count == 1 ? supported[0] : null;
    }

    public static string? Detect(ZipArchive archive)
    {
        return Detect(archive.Entries.Select(e => e.FullName));
    }

    public static bool IsZip(string? fileName)
    {
        return fileName?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static string GetName(string path)
    {
        var index = path.TrimEnd('/').LastIndexOf('/');
        return index < 0 ? path : path[(index + 1)..];
    }

    public static string? GetExtension(string? fileName)
    {
        return fileName == null ? null : Path.GetExtension(fileName).ToLowerInvariant();
    }
}

/// <summary>Архив для предпросмотра источника (test-source): без записи и без S3.</summary>
public class GoogleDriveTransientArchive(Stream stream, string? name, string? entryPath, ZipArchive? zip = null)
    : IFileArchive
{
    public WriteMode WriteMode => WriteMode.NotSupported;
    public string? Name => name;
    public string? EntryFilePath => entryPath;

    public Task<Stream?> ReadAsync(string entryName)
    {
        if (zip != null)
        {
            var entry = zip.GetEntry(entryName.TrimStart('/'));
            return Task.FromResult(entry?.Open());
        }

        return Task.FromResult(entryName == Name || entryName == EntryFilePath ? (Stream?)stream : null);
    }

    public Task<Stream?> ReadAsync()
    {
        return EntryFilePath == null ? Task.FromResult<Stream?>(null) : ReadAsync(EntryFilePath);
    }
}
