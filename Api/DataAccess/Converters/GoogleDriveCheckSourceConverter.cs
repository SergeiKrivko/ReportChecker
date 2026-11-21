using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class GoogleDriveCheckSourceConverter
{
    public static GoogleDriveCheckSource ToDomain(this GoogleDriveCheckSourceEntity entity)
    {
        return new GoogleDriveCheckSource
        {
            FileId = entity.FileId,
            ContentHash = entity.ContentHash,
            FileName = entity.FileName,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }

    public static GoogleDriveCheckSourceEntity ToEntity(this GoogleDriveCheckSource source, Guid id, Guid? checkId)
    {
        return new GoogleDriveCheckSourceEntity
        {
            Id = id,
            CheckId = checkId,
            FileId = source.FileId,
            ContentHash = source.ContentHash,
            FileName = source.FileName,
            CreatedAt = source.CreatedAt,
            DeletedAt = source.DeletedAt,
        };
    }
}
