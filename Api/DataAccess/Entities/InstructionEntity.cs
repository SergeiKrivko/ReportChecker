using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class InstructionEntity
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }
    public Guid UserId { get; init; } = Guid.Empty;
    public Guid? CommentId { get; init; }
    [MaxLength(1000)] public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public ReportEntity Report { get; init; } = null!;
    public CommentEntity? Comment { get; init; } = null!;
}