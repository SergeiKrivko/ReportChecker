namespace ReportChecker.Models;

public class SourceInfo
{
    public required SourceStatus Status { get; init; }
    public string? Format { get; init; }
}

public enum SourceStatus
{
    NotFound,
    WrongFormat,
    Success,
}