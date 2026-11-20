using System.IO.Compression;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.YandexDisk;

/// <summary>
/// Архив-снимок одного файла с Диска. Патчи создают новую версию-снимок в S3,
/// сам файл на Диске не изменяется (приложение работает только на чтение).
/// </summary>
public class YandexDiskFileArchive(
    IFileRepository fileRepository,
    ICheckSourceRepository<YandexDiskCheckSource> checkSourceRepository,
    CheckSource<YandexDiskCheckSource> source,
    Stream stream,
    string? entryPath) : IFileArchive
{
    public WriteMode WriteMode => WriteMode.Internal;
    public string? Name => source.Data.FileName;
    public string? EntryFilePath => entryPath ?? source.Data.FileName;

    public Task<Stream?> ReadAsync(string name)
    {
        return name == Name || name == EntryFilePath ? ReadAsync() : Task.FromResult<Stream?>(null);
    }

    public Task<Stream?> ReadAsync()
    {
        return Task.FromResult<Stream?>(stream);
    }

    public Task<CheckSourceUnion?> WriteAsync(Stream content, CancellationToken ct)
    {
        return WriteAsync(Name ?? "report", content, ct);
    }

    public async Task<CheckSourceUnion?> WriteAsync(string name, Stream content, CancellationToken ct)
    {
        var memory = new MemoryStream();
        await content.CopyToAsync(memory, ct);
        memory.Position = 0;

        var data = new YandexDiskCheckSource
        {
            Path = source.Data.Path,
            ContentHash = source.Data.ContentHash,
            FileName = source.Data.FileName,
            CreatedAt = DateTime.UtcNow,
        };
        var id = await checkSourceRepository.CreateAsync(null, data, ct);
        memory.Position = 0;
        await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, data.FileName!, memory);

        return new CheckSourceUnion
        {
            Id = id,
            YandexDisk = data,
        };
    }
}

/// <summary>Архив-снимок папки с Диска (zip с относительными путями файлов).</summary>
public class YandexDiskZipArchive(
    IFileRepository fileRepository,
    ICheckSourceRepository<YandexDiskCheckSource> checkSourceRepository,
    CheckSource<YandexDiskCheckSource> source,
    ZipArchive archive,
    Stream underlying,
    string? entryPath) : IFileArchive
{
    public WriteMode WriteMode => WriteMode.Internal;
    public string? Name => source.Data.FileName;
    public string? EntryFilePath => entryPath;

    public Task<Stream?> ReadAsync(string name)
    {
        var entry = archive.GetEntry(name.TrimStart('/'));
        return Task.FromResult(entry?.Open());
    }

    public Task<Stream?> ReadAsync()
    {
        return EntryFilePath == null ? Task.FromResult<Stream?>(null) : ReadAsync(EntryFilePath);
    }

    public Task<CheckSourceUnion?> WriteAsync(Stream content, CancellationToken ct)
    {
        if (EntryFilePath == null)
            throw new Exceptions.NotFoundException("No entry file");
        return WriteAsync(EntryFilePath, content, ct);
    }

    public async Task<CheckSourceUnion?> WriteAsync(string name, Stream content, CancellationToken ct)
    {
        var original = new MemoryStream();
        underlying.Position = 0;
        await underlying.CopyToAsync(original, ct);

        using (var zip = new ZipArchive(original, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry(name.TrimStart('/')) ??
                        throw new Exceptions.NotFoundException($"Файл '{name}' не найден в архиве");
            await using var entryStream = entry.Open();
            entryStream.SetLength(0);
            await content.CopyToAsync(entryStream, ct);
        }

        original.Position = 0;
        var data = new YandexDiskCheckSource
        {
            Path = source.Data.Path,
            ContentHash = source.Data.ContentHash,
            FileName = source.Data.FileName,
            CreatedAt = DateTime.UtcNow,
        };
        var id = await checkSourceRepository.CreateAsync(null, data, ct);
        await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, data.FileName!, original);

        return new CheckSourceUnion
        {
            Id = id,
            YandexDisk = data,
        };
    }
}
