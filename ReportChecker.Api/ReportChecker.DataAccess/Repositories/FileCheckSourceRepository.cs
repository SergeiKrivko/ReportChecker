using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class FileCheckSourceRepository(ReportCheckerDbContext dbContext)
    : BaseCheckSourceRepository<FileCheckSource, FileCheckSourceEntity>(dbContext.FileCheckSources, dbContext)
{
    protected override FileCheckSource FromEntity(FileCheckSourceEntity entity)
    {
        return new FileCheckSource
        {
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    protected override FileCheckSourceEntity ToEntity(Guid id, Guid? checkId, FileCheckSource data)
    {
        return new FileCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            FileName = data.FileName,
            CreatedAt = data.CreatedAt,
            DeletedAt = data.DeletedAt,
        };
    }
}