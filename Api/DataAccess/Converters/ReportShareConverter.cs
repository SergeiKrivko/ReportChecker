using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class ReportShareConverter
{
    public static ReportShare ToDomain(this ReportShareEntity entity)
    {
        return new ReportShare
        {
            ReportId = entity.ReportId,
            UserId = entity.UserId,
            AccessLevel = entity.AccessLevel,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static ReportShareLink ToDomain(this ReportShareLinkEntity entity)
    {
        return new ReportShareLink
        {
            ReportId = entity.ReportId,
            Token = entity.Token,
            CreatedAt = entity.CreatedAt,
        };
    }
}
