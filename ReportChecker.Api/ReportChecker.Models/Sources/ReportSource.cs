namespace ReportChecker.Models.Sources;

public class ReportSource<T>
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public required T Data { get; init; }
}