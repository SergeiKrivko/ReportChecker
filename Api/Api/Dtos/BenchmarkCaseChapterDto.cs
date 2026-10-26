namespace ReportChecker.Api.Dtos;

public class BenchmarkCaseChapterDto
{
    public required string Name { get; init; }

    public BenchmarkCaseLineDto[] Lines { get; init; } = [];
}