namespace ReportChecker.Api.Dtos;

public class ChapterDto
{
    public required string Name { get; init; }
    public required string Content { get; init; }
    public ChapterImageDto[] Images { get; init; } = [];
    public ChapterDto[] Children { get; init; } = [];
}