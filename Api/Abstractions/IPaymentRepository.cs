using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IPaymentRepository
{
    public Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default);
    public Task<Guid> CreatePaymentAsync(decimal amount, CancellationToken ct = default);
    public Task<bool> SetPaymentStatusAsync(Guid paymentId, PaymentStatus status, CancellationToken ct = default);
}