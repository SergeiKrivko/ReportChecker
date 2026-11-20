using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;
using ReportChecker.Models.YandexDisk;

namespace ReportChecker.SourceProviders.YandexDisk;

public class YandexDiskSourceProvider(
    IYandexDiskTokenService tokenService,
    IYandexDiskClient client,
    IYandexDiskSnapshotService snapshotService,
    IReportSourceRepository<YandexDiskReportSource> reportSourceRepository,
    ICheckSourceRepository<YandexDiskCheckSource> checkSourceRepository,
    ICheckRepository checkRepository,
    IFileRepository fileRepository,
    IConfiguration configuration) : ISourceProvider
{
    public string Key => "YandexDisk";

    public async Task<IFileArchive> OpenAsync(Guid reportId, Guid checkId)
    {
        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                           throw new NotFoundException("Report source not found");
        var checkSource = await checkSourceRepository.GetByCheckIdAsync(checkId) ??
                          throw new NotFoundException("Check source not found");
        return await OpenSnapshotAsync(checkSource, reportSource.Data.EntryFilePath);
    }

    public async Task<IFileArchive> OpenAsync(ReportSourceUnion source)
    {
        if (source.YandexDisk == null)
            throw new BadRequestException("YandexDisk source not set");

        var token = await tokenService.GetAccessTokenAsync(source.YandexDisk.OwnerId);
        var state = await client.GetStateAsync(token, source.YandexDisk.Path);
        var memory = await DownloadToMemoryAsync(token, state);

        if (!state.IsFolder && !YandexDiskEntryDetector.IsZip(state.Name))
        {
            return new YandexDiskTransientArchive(memory, state.Name,
                source.YandexDisk.EntryFilePath ?? state.Name);
        }

        var zip = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true);
        var entry = source.YandexDisk.EntryFilePath ??
                    (state.IsFolder ? state.SuggestedEntryPath : null) ??
                    YandexDiskEntryDetector.Detect(zip);
        return new YandexDiskTransientArchive(memory, state.IsFolder ? "sources.zip" : state.Name, entry, zip);
    }

    private async Task<MemoryStream> DownloadToMemoryAsync(string token, YandexDiskState state)
    {
        var maxBytes = long.TryParse(configuration["YandexDisk:MaxSnapshotBytes"], out var configured)
            ? configured
            : 200L * 1024 * 1024;
        if (state.Files.Sum(e => e.Size) > maxBytes)
            throw new BadRequestException("Источник на Яндекс.Диске слишком большой для предпросмотра");

        var memory = new MemoryStream();
        await client.DownloadAsync(token, state, memory);
        memory.Position = 0;
        return memory;
    }

    private async Task<IFileArchive> OpenSnapshotAsync(CheckSource<YandexDiskCheckSource> checkSource,
        string? entryFilePath)
    {
        var fileName = checkSource.Data.FileName;
        var stream = fileName == null
            ? await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, checkSource.Id)
            : await fileRepository.DownloadFileAsync(FileRepositoryBucket.Sources, checkSource.Id, fileName);

        if (!YandexDiskEntryDetector.IsZip(fileName))
            return new YandexDiskFileArchive(fileRepository, checkSourceRepository, checkSource, stream,
                entryFilePath ?? fileName);

        var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await stream.DisposeAsync();
        memory.Position = 0;

        var zip = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true);
        return new YandexDiskZipArchive(fileRepository, checkSourceRepository, checkSource, zip, memory,
            entryFilePath ?? YandexDiskEntryDetector.Detect(zip));
    }

    public async Task<SourceSchema> GetFirstSourceAsync(Guid reportId)
    {
        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                           throw new NotFoundException("Report source not found");

        var token = await tokenService.GetAccessTokenAsync(reportSource.Data.OwnerId);
        var state = await client.GetStateAsync(token, reportSource.Data.Path);
        var snapshot = await snapshotService.CreateSnapshotAsync(reportSource.Data.OwnerId,
            reportSource.Data.Path, state);

        return new SourceSchema(new CheckSourceUnion
        {
            Id = snapshot.Id,
            YandexDisk = snapshot.Data,
        }, state.Name);
    }

    public async Task<Guid> SaveAsync(Guid? checkId, CheckSourceUnion source)
    {
        if (source.YandexDisk == null)
            throw new BadRequestException("YandexDisk source not set");
        if (checkId == null)
            throw new BadRequestException("YandexDisk source can only be attached to a check");

        var check = await checkRepository.GetCheckByIdAsync(checkId.Value) ??
                    throw new NotFoundException("Check not found");
        var reportSource = await reportSourceRepository.GetByReportIdAsync(check.ReportId) ??
                           throw new NotFoundException("Report source not found");

        var token = await tokenService.GetAccessTokenAsync(reportSource.Data.OwnerId);
        var state = await client.GetStateAsync(token, source.YandexDisk.Path);
        var snapshot = await snapshotService.CreateSnapshotAsync(reportSource.Data.OwnerId,
            source.YandexDisk.Path, state, checkId);

        return snapshot.Id;
    }

    public Task<bool> AttachCheckAsync(Guid id, Guid checkId)
    {
        return checkSourceRepository.AttachAsync(id, checkId);
    }

    public async Task<Guid> SaveAsync(Guid reportId, ReportSourceUnion source)
    {
        if (source.YandexDisk == null)
            throw new BadRequestException("YandexDisk source not set");

        var token = await tokenService.GetAccessTokenAsync(source.YandexDisk.OwnerId);
        var state = await client.GetStateAsync(token, source.YandexDisk.Path);

        if (state.IsFolder)
        {
            var entry = source.YandexDisk.EntryFilePath ?? state.SuggestedEntryPath;
            if (entry == null)
                throw new BadRequestException(
                    "В папке несколько подходящих файлов — укажите главный файл (entryFilePath)");
        }
        else if (YandexDiskEntryDetector.GetExtension(state.Name) is not (".tex" or ".docx" or ".pdf" or ".zip"))
        {
            throw new BadRequestException(
                $"Формат '{state.Name}' не поддерживается: ожидается .tex, .docx, .pdf или .zip");
        }

        return await reportSourceRepository.CreateAsync(reportId, source.YandexDisk);
    }
}
