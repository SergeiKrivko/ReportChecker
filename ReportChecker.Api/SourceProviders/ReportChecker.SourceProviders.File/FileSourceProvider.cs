using System.IO.Compression;
using System.Text.Json;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.File;

public class FileSourceProvider(IFileRepository fileRepository) : ISourceProvider
{
    public string Key => "File";

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<IFileArchive> OpenAsync(string source)
    {
        var src = JsonSerializer.Deserialize<FileSourceSchema>(source, _options);
        if (src == null)
            throw new Exception("Invalid file schema " + source);
        var stream = await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, src.Id, src.FileName);
        if (src.FileName.EndsWith(".zip"))
        {
            return new ZipFileArchive(new ZipArchive(stream, ZipArchiveMode.Read), src.FileName);
        }

        return new SingleFileArchive(stream, src.FileName);
    }

    public async Task<SourceSchema> FindSourceAsync(string source)
    {
        return new SourceSchema(source, await OpenAsync(source));
    }
}