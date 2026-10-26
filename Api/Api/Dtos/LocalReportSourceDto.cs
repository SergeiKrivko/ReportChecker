namespace ReportChecker.Api.Dtos;

public class LocalReportSourceDto
{
    public required Guid InitialFileId { get; init; }
    public string? EntryFilePath { get; init; }
    public Guid ClientId { get; init; }
    public string? ClientMachineName { get; init; }
}