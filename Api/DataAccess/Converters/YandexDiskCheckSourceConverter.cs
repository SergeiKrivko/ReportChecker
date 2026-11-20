using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class YandexDiskCheckSourceConverter
{
    public static YandexDiskCheckSource ToDomain(this YandexDiskCheckSourceEntity entity)
    {
        return new YandexDiskCheckSource
        {
            Path = entity.Path,
            ContentHash = entity.ContentHash,
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    public static YandexDiskCheckSourceEntity ToEntity(this YandexDiskCheckSource source, Guid id, Guid? checkId)
    {
        return new YandexDiskCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            Path = source.Path,
            ContentHash = source.ContentHash,
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }
}
