namespace ReportChecker.Api.Dtos;

public class ChapterImageDto
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
}