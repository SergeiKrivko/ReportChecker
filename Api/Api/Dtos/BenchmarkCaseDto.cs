using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class BenchmarkCaseDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Format { get; init; }

    public string? EntryFile { get; init; }

    public string? Description { get; init; }

    public BenchmarkCaseDisplayMode DisplayMode { get; init; } = BenchmarkCaseDisplayMode.Collapsed;

    public BenchmarkExpectedIssueDto[] Expected { get; init; } = [];

    public string? ValidationError { get; init; }
}