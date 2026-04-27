using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class LocalReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<LocalReportSource, LocalReportSourceEntity, string>(dbContext.LocalReportSources,
        dbContext, name => f => f.EntryFilePath == name)
{
    protected override LocalReportSource FromEntity(LocalReportSourceEntity entity)
    {
        return new LocalReportSource
        {
            InitialFileId = entity.InitialFileId,
            EntryFilePath = entity.EntryFilePath,
            ClientId = entity.ClientId,
            ClientMachineName = entity.ClientMachineName
        };
    }

    protected override LocalReportSourceEntity ToEntity(Guid id, Guid reportId, LocalReportSource data)
    {
        return new LocalReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            InitialFileId = data.InitialFileId,
            EntryFilePath = data.EntryFilePath,
            ClientId = data.ClientId,
            ClientMachineName = data.ClientMachineName,
        };
    }
}