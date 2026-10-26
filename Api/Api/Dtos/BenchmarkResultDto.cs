using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class BenchmarkResultDto
{
    public required Guid Id { get; init; }

    public required Guid RunId { get; init; }

    public DateTime CreatedAt { get; init; }

    public int? ExpectedNumber { get; init; }

    public string? ErrorClass { get; init; }

    public string? Chapter { get; init; }

    public int? Line { get; init; }

    public string? ExpectedTitle { get; init; }
    public string? ExpectedComment { get; init; }
    public int? ExpectedPriority { get; init; }

    public int? FoundIndex { get; init; }

    public string? FoundTitle { get; init; }
    public string? FoundComment { get; init; }
    public int? FoundPriority { get; init; }

    public bool IsFound { get; init; }

    public bool? TitleMatch { get; init; }

    public bool? PriorityMatch { get; init; }

    public int? PriorityDelta { get; init; }

    public BenchmarkMatchMethod MatchingMethod { get; init; }

    public double? MatchScore { get; init; }

    public string? MatchingReason { get; init; }

    public BenchmarkFixMatchStatus FixMatchStatus { get; init; }

    public string? ExpectedFix { get; init; }
    public string? FoundFix { get; init; }
}