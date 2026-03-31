using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class FileReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<FileReportSource, FileReportSourceEntity, string>(dbContext.FileReportSources,
        dbContext, name => f => f.EntryFilePath == name)
{
    protected override FileReportSource FromEntity(FileReportSourceEntity entity)
    {
        return new FileReportSource
        {
            InitialFileId = entity.InitialFileId,
            EntryFilePath = entity.EntryFilePath,
        };
    }

    protected override FileReportSourceEntity ToEntity(Guid id, Guid reportId, FileReportSource data)
    {
        return new FileReportSourceEntity
        {
            Id = id,
            ReportId = reportId,
            InitialFileId = data.InitialFileId,
            EntryFilePath = data.EntryFilePath,
        };
    }
}