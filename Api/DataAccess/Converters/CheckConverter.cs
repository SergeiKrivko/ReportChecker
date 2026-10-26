using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class CheckConverter
{
    public static Check ToDomain(this CheckEntity entity)
    {
        return new Check
        {
            Id = entity.CheckId,
            ReportId = entity.ReportId,
            UserId = entity.UserId,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status,
        };
    }
}