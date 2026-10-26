using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class LocalCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<LocalCheckSource, LocalCheckSourceEntity>(dbContext.LocalCheckSources, dbContext)
{
    protected override LocalCheckSource FromEntity(LocalCheckSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override LocalCheckSourceEntity ToEntity(Guid id, Guid? checkId, LocalCheckSource data)
    {
        return data.ToEntity(id, checkId);
    }
}