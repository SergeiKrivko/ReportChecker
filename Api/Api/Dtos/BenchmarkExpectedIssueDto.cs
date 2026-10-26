namespace ReportChecker.Api.Dtos;

public class BenchmarkExpectedIssueDto
{
    public required int Number { get; init; }

    public required string Chapter { get; init; }

    public int? Line { get; init; }

    public required string Title { get; init; }

    public required string Comment { get; init; }

    public int Priority { get; init; } = 1;

    public string? ErrorClass { get; init; }

    public bool CheckFix { get; init; }

    public PatchLineDto[]? Patch { get; init; }
}