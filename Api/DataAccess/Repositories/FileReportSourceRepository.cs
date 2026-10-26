using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class FileReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<FileReportSource, FileReportSourceEntity, string>(dbContext.FileReportSources,
        dbContext, name => f => f.EntryFilePath == name)
{
    protected override FileReportSource FromEntity(FileReportSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override FileReportSourceEntity ToEntity(Guid id, Guid reportId, FileReportSource data)
    {
        return data.ToEntity(id, reportId);
    }
}