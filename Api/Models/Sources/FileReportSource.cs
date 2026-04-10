namespace ReportChecker.Models.Sources;

public class FileReportSource
{
    public required Guid InitialFileId { get; init; }
    public string? EntryFilePath { get; init; }
}