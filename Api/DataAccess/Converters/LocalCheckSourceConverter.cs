using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class LocalCheckSourceConverter
{
    public static LocalCheckSource ToDomain(this LocalCheckSourceEntity entity)
    {
        return new LocalCheckSource
        {
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    public static LocalCheckSourceEntity ToEntity(this LocalCheckSource source, Guid id, Guid? checkId)
    {
        return new LocalCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }
}