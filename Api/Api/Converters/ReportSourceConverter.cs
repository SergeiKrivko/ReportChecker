using ReportChecker.Api.Dtos;
using ReportChecker.Exceptions;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Converters;

public static class ReportSourceConverter
{
    public static ReportSourceUnionDto? ToDto(this ReportSourceUnion? source)
    {
        return source == null
            ? null
            : new ReportSourceUnionDto
            {
                GitHub = source.GitHub?.ToDto(),
                File = source.File?.ToDto(),
                Local = source.Local?.ToDto(),
                YandexDisk = source.YandexDisk?.ToDto(),
                GoogleDrive = source.GoogleDrive?.ToDto(),
            };
    }

    public static FileReportSourceDto ToDto(this FileReportSource source)
    {
        return new FileReportSourceDto
        {
            InitialFileId = source.InitialFileId,
            EntryFilePath = source.EntryFilePath,
        };
    }

    public static GitHubReportSourceDto ToDto(this GitHubReportSource source)
    {
        return new GitHubReportSourceDto
        {
            RepositoryId = source.RepositoryId,
            Branch = source.Branch,
            Path = source.Path,
        };
    }

    public static YandexDiskReportSourceDto ToDto(this YandexDiskReportSource source)
    {
        return new YandexDiskReportSourceDto
        {
            Path = source.Path,
            EntryFilePath = source.EntryFilePath,
        };
    }

    public static GoogleDriveReportSourceDto ToDto(this GoogleDriveReportSource source)
    {
        return new GoogleDriveReportSourceDto
        {
            FileId = source.FileId,
            Name = source.Name,
            EntryFilePath = source.EntryFilePath,
        };
    }

    public static LocalReportSourceDto ToDto(this LocalReportSource source)
    {
        return new LocalReportSourceDto
        {
            InitialFileId = source.InitialFileId,
            EntryFilePath = source.EntryFilePath,
            ClientId = source.ClientId,
            ClientMachineName = source.ClientMachineName,
        };
    }

    /// <param name="diskOwnerId">
    /// Владелец Диска для источников YandexDisk и GoogleDrive: подставляется сервером
    /// из JWT, значение из запроса не используется.
    /// </param>
    public static ReportSourceUnion ToDomain(this ReportSourceUnionDto dto, Guid? diskOwnerId = null)
    {
        return new ReportSourceUnion
        {
            GitHub = dto.GitHub?.ToDomain(),
            File = dto.File?.ToDomain(),
            Local = dto.Local?.ToDomain(),
            YandexDisk = dto.YandexDisk?.ToDomain(diskOwnerId),
            GoogleDrive = dto.GoogleDrive?.ToDomain(diskOwnerId),
        };
    }

    public static FileReportSource ToDomain(this FileReportSourceDto dto)
    {
        return new FileReportSource
        {
            InitialFileId = dto.InitialFileId,
            EntryFilePath = dto.EntryFilePath,
        };
    }

    public static GitHubReportSource ToDomain(this GitHubReportSourceDto dto)
    {
        return new GitHubReportSource
        {
            RepositoryId = dto.RepositoryId,
            Branch = dto.Branch,
            Path = dto.Path,
        };
    }

    public static YandexDiskReportSource ToDomain(this YandexDiskReportSourceDto dto, Guid? ownerId)
    {
        if (ownerId == null)
            throw new BadRequestException("Для источника YandexDisk не определён владелец Диска");

        return new YandexDiskReportSource
        {
            OwnerId = ownerId.Value,
            Path = dto.Path,
            EntryFilePath = dto.EntryFilePath,
        };
    }

    public static GoogleDriveReportSource ToDomain(this GoogleDriveReportSourceDto dto, Guid? ownerId)
    {
        if (ownerId == null)
            throw new BadRequestException("Для источника GoogleDrive не определён владелец Диска");

        return new GoogleDriveReportSource
        {
            OwnerId = ownerId.Value,
            FileId = dto.FileId,
            Name = dto.Name,
            EntryFilePath = dto.EntryFilePath,
        };
    }

    public static LocalReportSource ToDomain(this LocalReportSourceDto dto)
    {
        return new LocalReportSource
        {
            InitialFileId = dto.InitialFileId,
            EntryFilePath = dto.EntryFilePath,
            ClientId = dto.ClientId,
            ClientMachineName = dto.ClientMachineName,
        };
    }
}