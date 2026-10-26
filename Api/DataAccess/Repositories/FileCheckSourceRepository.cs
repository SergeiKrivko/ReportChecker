using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class FileCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<FileCheckSource, FileCheckSourceEntity>(dbContext.FileCheckSources, dbContext)
{
    protected override FileCheckSource FromEntity(FileCheckSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override FileCheckSourceEntity ToEntity(Guid id, Guid? checkId, FileCheckSource data)
    {
        return data.ToEntity(id, checkId);
    }
}