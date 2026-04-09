using System.IO.Compression;
using ReportChecker.Abstractions;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.File;

public class FileSourceProvider(
    IFileRepository fileRepository,
    IReportSourceRepository<FileReportSource> reportSourceRepository,
    ICheckSourceRepository<FileCheckSource> checkSourceRepository) : ISourceProvider
{
    public string Key => "File";

    public async Task<IFileArchive> OpenAsync(Guid reportId, Guid checkId)
    {
        var source = await checkSourceRepository.GetByCheckIdAsync(checkId) ??
                     throw new Exception("Check source not found");
        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                           throw new Exception("Report source not found");
        return await OpenAsync(reportSource.Data, source);
    }

    public async Task<IFileArchive> OpenAsync(ReportSourceUnion reportSource)
    {
        if (reportSource.File == null)
            throw new Exception("File source not set");
        var checkSource = await checkSourceRepository.GetByIdAsync(reportSource.File.InitialFileId);
        if (checkSource == null)
            throw new Exception("Referenced check source not found");
        return await OpenAsync(reportSource.File, checkSource);
    }

    private async Task<IFileArchive> OpenAsync(FileReportSource reportSource, CheckSource<FileCheckSource> checkSource)
    {
        var stream = checkSource.Data.FileName == null
            ? await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, checkSource.Id)
            : await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, checkSource.Id,
                checkSource.Data.FileName);
        if (checkSource.Data.FileName?.EndsWith(".zip") ?? false)
        {
            return new ZipFileArchive(checkSourceRepository, fileRepository, checkSource.Id,
                new ZipArchive(stream, ZipArchiveMode.Read),
                checkSource.Data.FileName, reportSource.EntryFilePath);
        }

        return new SingleFileArchive(stream, checkSource.Data.FileName);
    }

    public async Task<SourceSchema> GetFirstSourceAsync(Guid reportId)
    {
        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                           throw new Exception("Report source not found");

        var source = await checkSourceRepository.GetByIdAsync(reportSource.Data.InitialFileId);
        if (source == null)
            throw new Exception("Referenced check source not found");

        return new SourceSchema(new CheckSourceUnion { File = source.Data, Id = source.Id }, source.Data.FileName);
    }

    public async Task<Guid> SaveAsync(Guid? checkId, CheckSourceUnion source)
    {
        if (source.File == null)
            throw new Exception("File source not set");
        return await checkSourceRepository.CreateAsync(checkId, source.File);
    }

    public async Task<Guid> SaveAsync(Guid id, Guid? checkId, CheckSourceUnion source)
    {
        if (source.File == null)
            throw new Exception("File source not set");
        return await checkSourceRepository.CreateAsync(id, checkId, source.File);
    }

    public async Task<bool> AttachCheckAsync(Guid id, Guid checkId)
    {
        return await checkSourceRepository.AttachAsync(id, checkId);
    }

    public async Task<Guid> SaveAsync(Guid reportId, ReportSourceUnion source)
    {
        if (source.File == null)
            throw new Exception("File source not set");
        return await reportSourceRepository.CreateAsync(reportId, source.File);
    }
}