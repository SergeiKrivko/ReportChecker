namespace ReportChecker.Models;

public class SubscriptionPlan
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int TokensLimit { get; init; }
    public int ReportsLimit { get; init; }
    public bool IsHidden { get; init; }
    public IReadOnlyList<SubscriptionOffer> Offers { get; init; } = [];

    public required DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}