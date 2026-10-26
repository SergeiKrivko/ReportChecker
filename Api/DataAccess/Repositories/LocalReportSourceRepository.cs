using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public class LocalReportSourceRepository(ReportCheckerDbContext dbContext)
    : BaseReportSourceRepository<LocalReportSource, LocalReportSourceEntity, string>(dbContext.LocalReportSources,
        dbContext, name => f => f.EntryFilePath == name)
{
    protected override LocalReportSource FromEntity(LocalReportSourceEntity entity)
    {
        return entity.ToDomain();
    }

    protected override LocalReportSourceEntity ToEntity(Guid id, Guid reportId, LocalReportSource data)
    {
        return data.ToEntity(id, reportId);
    }
}