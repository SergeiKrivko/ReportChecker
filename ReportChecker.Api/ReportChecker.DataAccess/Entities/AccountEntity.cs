using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class AccountEntity
{
    public Guid AccountId { get; init; }
    public required Guid UserId { get; init; }
    [MaxLength(20)] public required string Provider { get; init; }
    [MaxLength(200)] public required string ProviderUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public virtual UserEntity User { get; init; } = null!;
}