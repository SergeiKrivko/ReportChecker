using ReportChecker.Api.Dtos;
using ReportChecker.SourceProviders.GitHub.Models;

namespace ReportChecker.Api.Converters;

public static class RepositoryConverter
{
    public static RepositoryDto ToDto(this Repository repository)
    {
        return new RepositoryDto
        {
            Id = repository.Id,
            Name = repository.Name,
        };
    }

    public static RepositoryFileDto ToDto(this RepositoryFile file)
    {
        return new RepositoryFileDto
        {
            Name = file.Name,
            Path = file.Path,
        };
    }

    public static RepositoryInfoDto ToDto(this RepositoryInfo repository)
    {
        return new RepositoryInfoDto
        {
            Id = repository.Id,
            Name = repository.Name,
            Url = repository.Url,
        };
    }
}