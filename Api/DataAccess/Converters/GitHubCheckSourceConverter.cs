using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class GitHubCheckSourceConverter
{
    public static GitHubCheckSource ToDomain(this GitHubCheckSourceEntity entity)
    {
        return new GitHubCheckSource
        {
            CommitHash = entity.CommitHash,
        };
    }

    public static GitHubCheckSourceEntity ToEntity(this GitHubCheckSource source, Guid id, Guid? checkId)
    {
        return new GitHubCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            CommitHash = source.CommitHash,
        };
    }
}