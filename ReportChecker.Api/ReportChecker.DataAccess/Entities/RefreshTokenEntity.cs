using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class RefreshTokenEntity
{
    public Guid UserId { get; init; }
    [MaxLength(50)] public required string Token { get; init; }

    public virtual UserEntity User { get; init; } = null!;
}