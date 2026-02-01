namespace ReportChecker.Models;

public class Account
{
    public Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}