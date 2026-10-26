using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class FileReportSourceConverter
{
    public static FileReportSource ToDomain(this FileReportSourceEntity entity)
    {
        return new FileReportSource
        {
            InitialFileId = entity.InitialFileId,
            EntryFilePath = entity.EntryFilePath,
        };
    }

    public static FileReportSourceEntity ToEntity(this FileReportSource source, Guid id, Guid reportId)
    {
        return new FileReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            InitialFileId = source.InitialFileId,
            EntryFilePath = source.EntryFilePath,
        };
    }
}