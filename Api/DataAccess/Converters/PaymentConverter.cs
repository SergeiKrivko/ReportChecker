using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

internal static class PaymentConverter
{
    public static Payment ToDomain(this PaymentEntity entity)
    {
        return new Payment
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
        };
    }

    public static PaymentEntity ToEntity(this Payment domain)
    {
        return new PaymentEntity
        {
            Id = domain.Id,
            Amount = domain.Amount,
            Status = domain.Status,
            CreatedAt = domain.CreatedAt,
        };
    }
}