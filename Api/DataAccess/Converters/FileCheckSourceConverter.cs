using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class FileCheckSourceConverter
{
    public static FileCheckSource ToDomain(this FileCheckSourceEntity entity)
    {
        return new FileCheckSource
        {
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    public static FileCheckSourceEntity ToEntity(this FileCheckSource source, Guid id, Guid? checkId)
    {
        return new FileCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }
}