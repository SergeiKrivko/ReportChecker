using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class GoogleDriveCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<GoogleDriveCheckSource, GoogleDriveCheckSourceEntity>(dbContext.GoogleDriveCheckSources,
        dbContext)
{
    protected override GoogleDriveCheckSource FromEntity(GoogleDriveCheckSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override GoogleDriveCheckSourceEntity ToEntity(Guid id, Guid? checkId, GoogleDriveCheckSource data)
    {
        return data.ToEntity(id, checkId);
    }
}
