namespace ReportChecker.Models.Sources;

public class LocalReportSource
{
    public required Guid InitialFileId { get; init; }
    public string? EntryFilePath { get; init; }
    public Guid ClientId { get; init; }
    public string? ClientMachineName { get; init; }
}