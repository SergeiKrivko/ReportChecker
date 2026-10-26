using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess.Converters;
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
                Data = entity.ToDomain()
            };
    }

    protected override GitHubReportSource FromEntity(GitHubReportSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override GitHubReportSourceEntity ToEntity(Guid id, Guid reportId, GitHubReportSource data)
    {
        return data.ToEntity(id, reportId);
    }
}