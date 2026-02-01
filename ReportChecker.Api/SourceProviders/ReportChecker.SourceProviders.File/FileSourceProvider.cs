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

    public async Task<Stream> GetStreamAsync(string source)
    {
        var src = JsonSerializer.Deserialize<FileSourceSchema>(source, _options);
        if (src == null)
            throw new Exception("Invalid file schema " + source);
        return await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, src.Id, src.FileName);
    }

    public async Task<SourceSchema> FindSourceAsync(string source)
    {
        return new SourceSchema(source, await GetStreamAsync(source));
    }
}