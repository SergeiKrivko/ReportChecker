using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class GitHubCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<GitHubCheckSource, GitHubCheckSourceEntity>(dbContext.GitHubCheckSources, dbContext)
{
    protected override GitHubCheckSource FromEntity(GitHubCheckSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override GitHubCheckSourceEntity ToEntity(Guid id, Guid? checkId, GitHubCheckSource data)
    {
        return data.ToEntity(id, checkId);
    }
}