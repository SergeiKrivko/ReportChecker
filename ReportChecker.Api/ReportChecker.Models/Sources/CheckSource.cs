namespace ReportChecker.Models.Sources;

public class CheckSource<T>
{
    public required Guid Id { get; init; }
    public required Guid? CheckId { get; init; }
    public required T Data { get; init; }
}