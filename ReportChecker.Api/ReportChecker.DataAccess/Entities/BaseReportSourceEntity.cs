namespace ReportChecker.DataAccess.Entities;

public class BaseReportSourceEntity
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }

    public virtual ReportEntity Report { get; init; } = null!;
}