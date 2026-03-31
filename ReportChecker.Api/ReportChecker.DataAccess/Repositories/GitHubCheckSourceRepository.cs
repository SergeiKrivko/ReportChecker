using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class GitHubCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<GitHubCheckSource, GitHubCheckSourceEntity>(dbContext.GitHubCheckSources, dbContext)
{
    protected override GitHubCheckSource FromEntity(GitHubCheckSourceEntity entity)
    {
        return new GitHubCheckSource
        {
            CommitHash = entity.CommitHash
        };
    }

    protected override GitHubCheckSourceEntity ToEntity(Guid id, Guid? checkId, GitHubCheckSource data)
    {
        return new GitHubCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            CommitHash = data.CommitHash
        };
    }
}