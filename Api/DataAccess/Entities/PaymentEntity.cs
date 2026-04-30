using ReportChecker.Models;

namespace ReportChecker.DataAccess.Entities;

public class PaymentEntity
{
    public required Guid Id { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }

    public virtual UserSubscriptionEntity? UserSubscription { get; init; }
}