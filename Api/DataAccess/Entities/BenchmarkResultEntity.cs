using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class BenchmarkResultEntity
{
    public required Guid Id { get; init; }
    public required Guid RunId { get; init; }
    public required DateTime CreatedAt { get; init; }

    // Эталонная (известная) ошибка; ExpectedNumber == null для «лишних» найденных ошибок.
    public int? ExpectedNumber { get; init; }
    [MaxLength(64)] public string? ErrorClass { get; init; }
    [MaxLength(256)] public string? Chapter { get; init; }
    public int? Line { get; init; }
    [MaxLength(500)] public string? ExpectedTitle { get; init; }
    public string? ExpectedComment { get; init; }
    public int? ExpectedPriority { get; init; }

    // Найденная моделью ошибка; FoundIndex == null если известная ошибка не найдена.
    public int? FoundIndex { get; init; }
    [MaxLength(500)] public string? FoundTitle { get; init; }
    public string? FoundComment { get; init; }
    public int? FoundPriority { get; init; }

    public bool IsFound { get; init; }
    public bool? TitleMatch { get; init; }
    public bool? PriorityMatch { get; init; }
    public int? PriorityDelta { get; init; }
    public BenchmarkMatchMethod MatchingMethod { get; init; }
    public double? MatchScore { get; init; }
    [MaxLength(1000)] public string? MatchingReason { get; init; }

    public BenchmarkFixMatchStatus FixMatchStatus { get; init; }
    public string? ExpectedFix { get; init; }
    public string? FoundFix { get; init; }

    public virtual BenchmarkRunEntity Run { get; init; } = null!;
}
