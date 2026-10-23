using System.ComponentModel.DataAnnotations;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class LlmModelEntity
{
    public required Guid Id { get; init; }
    [MaxLength(64)] public required string DisplayName { get; init; }
    [MaxLength(64)] public required string ModelKey { get; init; }
    public double InputCoefficient { get; init; }
    public double OutputCoefficient { get; init; }
    public LlmReasoningEffort DefaultReasoningLevel { get; init; } = LlmReasoningEffort.Default;
    public bool IsRecommended { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public virtual ICollection<ReportEntity> Reports { get; init; } = [];
    public virtual ICollection<LlmUsageEntity> Usages { get; init; } = [];
}