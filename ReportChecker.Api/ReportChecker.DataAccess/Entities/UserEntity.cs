namespace ReportChecker.DataAccess.Entities;

public class UserEntity
{
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    public virtual IEnumerable<AccountEntity> Accounts { get; init; } = null!;
    public virtual IEnumerable<ReportEntity> Reports { get; init; } = null!;
    public virtual IEnumerable<CheckEntity> Checks { get; init; } = null!;
    public virtual IEnumerable<RefreshTokenEntity> RefreshTokens { get; init; } = null!;
}