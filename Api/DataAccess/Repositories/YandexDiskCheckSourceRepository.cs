using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class YandexDiskCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<YandexDiskCheckSource, YandexDiskCheckSourceEntity>(dbContext.YandexDiskCheckSources,
        dbContext)
{
    protected override YandexDiskCheckSource FromEntity(YandexDiskCheckSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override YandexDiskCheckSourceEntity ToEntity(Guid id, Guid? checkId, YandexDiskCheckSource data)
    {
        return data.ToEntity(id, checkId);
    }
}
