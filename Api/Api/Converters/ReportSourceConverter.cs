using ReportChecker.Api.Dtos;
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

    public static ReportSourceUnion ToDomain(this ReportSourceUnionDto dto)
    {
        return new ReportSourceUnion
        {
            GitHub = dto.GitHub?.ToDomain(),
            File = dto.File?.ToDomain(),
            Local = dto.Local?.ToDomain(),
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