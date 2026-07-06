namespace ReportChecker.Studio.Models;

public class Limit<T>
{
    public required T Maximum { get; init; }
    public required T Current { get; init; }
}