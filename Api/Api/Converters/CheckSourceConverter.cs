using ReportChecker.Api.Dtos;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Converters;

public static class CheckSourceConverter
{
    public static CheckSourceUnionDto ToDto(this CheckSourceUnion source)
    {
        return new CheckSourceUnionDto
        {
            Id = source.Id,
            GitHub = source.GitHub?.ToDto(),
            File = source.File?.ToDto(),
            Local = source.Local?.ToDto(),
        };
    }

    public static FileCheckSourceDto ToDto(this FileCheckSource source)
    {
        return new FileCheckSourceDto
        {
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }

    public static GitHubCheckSourceDto ToDto(this GitHubCheckSource source)
    {
        return new GitHubCheckSourceDto
        {
            CommitHash = source.CommitHash,
        };
    }

    public static LocalCheckSourceDto ToDto(this LocalCheckSource source)
    {
        return new LocalCheckSourceDto
        {
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }

    public static CheckSourceUnion ToDomain(this CheckSourceUnionDto dto)
    {
        return new CheckSourceUnion
        {
            Id = dto.Id,
            GitHub = dto.GitHub?.ToDomain(),
            File = dto.File?.ToDomain(),
            Local = dto.Local?.ToDomain(),
        };
    }

    public static FileCheckSource ToDomain(this FileCheckSourceDto dto)
    {
        return new FileCheckSource
        {
            FileName = dto.FileName,
            CreatedAt = dto.CreatedAt,
            DeletedAt = dto.DeletedAt,
        };
    }

    public static GitHubCheckSource ToDomain(this GitHubCheckSourceDto dto)
    {
        return new GitHubCheckSource
        {
            CommitHash = dto.CommitHash,
        };
    }

    public static LocalCheckSource ToDomain(this LocalCheckSourceDto dto)
    {
        return new LocalCheckSource
        {
            FileName = dto.FileName,
            CreatedAt = dto.CreatedAt,
            DeletedAt = dto.DeletedAt,
        };
    }
}