namespace ReportChecker.Shared.Models;

public struct FilePosition
{
    public required string Path { get; init; }
    public required int Line { get; init; }
}