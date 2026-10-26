namespace ReportChecker.Api.Dtos;

public class BenchmarkCaseTextDto
{
    public required string CaseId { get; init; }

    public required string CaseName { get; init; }

    public required string Format { get; init; }

    public BenchmarkCaseChapterDto[] Chapters { get; init; } = [];
}