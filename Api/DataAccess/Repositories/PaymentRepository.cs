using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class PaymentRepository(ReportCheckerDbContext dbContext) : IPaymentRepository
{
    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId, CancellationToken ct = default)
    {
        var entity = await dbContext.Payments
            .Where(e => e.Id == paymentId)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<Guid> CreatePaymentAsync(decimal amount, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new PaymentEntity
        {
            Id = id,
            Amount = amount,
            Status = PaymentStatus.Wait,
            CreatedAt = DateTime.UtcNow,
        };
        await dbContext.Payments.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> SetPaymentStatusAsync(Guid paymentId, PaymentStatus status, CancellationToken ct = default)
    {
        var count = await dbContext.Payments
            .Where(e => e.Id == paymentId)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.Status, status), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }
}