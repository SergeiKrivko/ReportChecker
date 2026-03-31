namespace ReportChecker.DataAccess.Entities;

public class BaseCheckSourceEntity
{
    public Guid Id { get; init; }
    public Guid? CheckId { get; init; }

    public CheckEntity? Check { get; init; }
}