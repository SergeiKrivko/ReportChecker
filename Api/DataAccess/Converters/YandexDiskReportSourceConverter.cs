using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class YandexDiskReportSourceConverter
{
    public static YandexDiskReportSource ToDomain(this YandexDiskReportSourceEntity entity)
    {
        return new YandexDiskReportSource
        {
            OwnerId = entity.OwnerId,
            Path = entity.Path,
            EntryFilePath = entity.EntryFilePath,
        };
    }

    public static YandexDiskReportSourceEntity ToEntity(this YandexDiskReportSource source, Guid id, Guid reportId)
    {
        return new YandexDiskReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            OwnerId = source.OwnerId,
            Path = source.Path,
            EntryFilePath = source.EntryFilePath,
        };
    }
}
