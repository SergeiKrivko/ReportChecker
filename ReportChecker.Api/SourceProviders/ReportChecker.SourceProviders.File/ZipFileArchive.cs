using System.IO.Compression;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.File;

public class ZipFileArchive(
    ICheckSourceRepository<FileCheckSource> checkSourceRepository,
    IFileRepository fileRepository,
    Guid fileId,
    ZipArchive archive,
    string zipName,
    string? entryFile = null) : IFileArchive
{
    public WriteMode WriteMode => WriteMode.Internal;
    public string Name => zipName;
    public string? EntryFilePath => entryFile;

    public async Task<Stream?> ReadAsync(string name)
    {
        name = name.TrimStart('/');
        var entry = archive.GetEntry(name);
        if (entry == null)
            return null;
        return await entry.OpenAsync();
    }

    public async Task<Stream?> ReadAsync() => entryFile == null ? null : await ReadAsync(entryFile);

    public async Task<CheckSourceUnion?> WriteAsync(string name, Stream content, CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await using (var stream = await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, fileId, zipName))
        {
            await stream.CopyToAsync(memoryStream, ct);
        }

        var zip = new ZipArchive(memoryStream, ZipArchiveMode.Update);
        var entry = zip.GetEntry(name);
        if (entry == null)
            throw new Exception("No entry file");
        await using var entryStream = await entry.OpenAsync(ct);
        await content.CopyToAsync(entryStream, ct);
        await zip.DisposeAsync();

        await using var newFileStream = new MemoryStream(memoryStream.ToArray());

        var source = new FileCheckSource
        {
            FileName = zipName,
            CreatedAt = DateTime.UtcNow,
        };
        var id = await checkSourceRepository.CreateAsync(null, source, ct);
        await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, zipName, newFileStream);
        return new CheckSourceUnion
        {
            Id = id,
            File = source,
        };
    }

    public async Task<CheckSourceUnion?> WriteAsync(Stream content, CancellationToken ct)
    {
        if (entryFile == null)
            throw new Exception("No entry file");
        return await WriteAsync(entryFile, content, ct);
    }
}