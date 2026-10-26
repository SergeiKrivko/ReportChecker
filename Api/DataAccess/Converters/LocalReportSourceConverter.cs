using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Converters;

public static class LocalReportSourceConverter
{
    public static LocalReportSource ToDomain(this LocalReportSourceEntity entity)
    {
        return new LocalReportSource
        {
            InitialFileId = entity.InitialFileId,
            EntryFilePath = entity.EntryFilePath,
            ClientId = entity.ClientId,
            ClientMachineName = entity.ClientMachineName,
        };
    }

    public static LocalReportSourceEntity ToEntity(this LocalReportSource source, Guid id, Guid reportId)
    {
        return new LocalReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            InitialFileId = source.InitialFileId,
            EntryFilePath = source.EntryFilePath,
            ClientId = source.ClientId,
            ClientMachineName = source.ClientMachineName,
        };
    }
}