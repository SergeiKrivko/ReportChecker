using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class YandexDiskReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<YandexDiskReportSource, YandexDiskReportSourceEntity, Guid>(
        dbContext.YandexDiskReportSources, dbContext, ownerId => e => e.OwnerId == ownerId)
{
    protected override YandexDiskReportSource FromEntity(YandexDiskReportSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override YandexDiskReportSourceEntity ToEntity(Guid id, Guid reportId, YandexDiskReportSource data)
    {
        return data.ToEntity(id, reportId);
    }
}
