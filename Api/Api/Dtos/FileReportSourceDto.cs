namespace ReportChecker.Api.Dtos;

public class FileReportSourceDto
{
    public required Guid InitialFileId { get; init; }
    public string? EntryFilePath { get; init; }
}