using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class GitHubReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<GitHubReportSource, GitHubReportSourceEntity, long>(dbContext.GitHubReportSources,
        dbContext, repositoryId => e => e.RepositoryId == repositoryId)
{
    public async Task<ReportSource<GitHubReportSource>?> GetByRepositoryIdAsync(long repositoryId,
        CancellationToken ct = default)
    {
        var entity = await dbContext.GitHubReportSources
            .Where(e => e.RepositoryId == repositoryId)
            .FirstOrDefaultAsync(ct);
        return entity == null
            ? null
            : new ReportSource<GitHubReportSource>
            {
                Id = entity.Id,
                ReportId = entity.ReportId,
                Data = FromEntity(entity)
            };
    }

    protected override GitHubReportSource FromEntity(GitHubReportSourceEntity entity)
    {
        return new GitHubReportSource
        {
            RepositoryId = entity.RepositoryId,
            Branch = entity.Branch,
            Path = entity.Path,
        };
    }

    protected override GitHubReportSourceEntity ToEntity(Guid id, Guid reportId, GitHubReportSource data)
    {
        return new GitHubReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            RepositoryId = data.RepositoryId,
            Branch = data.Branch,
            Path = data.Path,
        };
    }
}