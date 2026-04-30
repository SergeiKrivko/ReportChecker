namespace ReportChecker.Models;

public class Payment
{
    public required Guid Id { get; init; }
    public required decimal Amount { get; init; }
    public required PaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}