using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class GitHubReportSourceConverter
{
    public static GitHubReportSource ToDomain(this GitHubReportSourceEntity entity)
    {
        return new GitHubReportSource
        {
            RepositoryId = entity.RepositoryId,
            Branch = entity.Branch,
            Path = entity.Path,
        };
    }

    public static GitHubReportSourceEntity ToEntity(this GitHubReportSource source, Guid id, Guid reportId)
    {
        return new GitHubReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            RepositoryId = source.RepositoryId,
            Branch = source.Branch,
            Path = source.Path,
        };
    }
}