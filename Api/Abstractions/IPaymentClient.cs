namespace ReportChecker.Abstractions;

public interface IPaymentClient
{
    public Task<string> CreatePaymentAsync(decimal sum, Guid paymentId, CancellationToken ct = default);
    public Task<bool> IsPaymentSuccessfulAsync(Guid paymentId, decimal expectedAmount,
        CancellationToken ct = default);
}