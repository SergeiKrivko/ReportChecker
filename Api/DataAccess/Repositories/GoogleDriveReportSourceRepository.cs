using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class GoogleDriveReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<GoogleDriveReportSource, GoogleDriveReportSourceEntity, Guid>(
        dbContext.GoogleDriveReportSources, dbContext, ownerId => e => e.OwnerId == ownerId)
{
    protected override GoogleDriveReportSource FromEntity(GoogleDriveReportSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override GoogleDriveReportSourceEntity ToEntity(Guid id, Guid reportId, GoogleDriveReportSource data)
    {
        return data.ToEntity(id, reportId);
    }
}
