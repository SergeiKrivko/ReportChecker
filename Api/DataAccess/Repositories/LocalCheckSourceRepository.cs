using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class LocalCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<LocalCheckSource, LocalCheckSourceEntity>(dbContext.LocalCheckSources, dbContext)
{
    protected override LocalCheckSource FromEntity(LocalCheckSourceEntity entity)
    {
        return new LocalCheckSource
        {
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    protected override LocalCheckSourceEntity ToEntity(Guid id, Guid? checkId, LocalCheckSource data)
    {
        return new LocalCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            FileName = data.FileName,
            CreatedAt = data.CreatedAt,
            DeletedAt = data.DeletedAt,
        };
    }
}