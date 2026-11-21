using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class GoogleDriveReportSourceConverter
{
    public static GoogleDriveReportSource ToDomain(this GoogleDriveReportSourceEntity entity)
    {
        return new GoogleDriveReportSource
        {
            OwnerId = entity.OwnerId,
            FileId = entity.FileId,
            Name = entity.Name,
            EntryFilePath = entity.EntryFilePath,
        };
    }

    public static GoogleDriveReportSourceEntity ToEntity(this GoogleDriveReportSource source, Guid id, Guid reportId)
    {
        return new GoogleDriveReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            OwnerId = source.OwnerId,
            FileId = source.FileId,
            Name = source.Name,
            EntryFilePath = source.EntryFilePath,
        };
    }
}
