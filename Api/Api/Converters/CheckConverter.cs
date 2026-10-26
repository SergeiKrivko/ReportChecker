using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class CheckConverter
{
    public static CheckDto ToDto(this Check check)
    {
        return new CheckDto
        {
            Id = check.Id,
            ReportId = check.ReportId,
            UserId = check.UserId,
            Name = check.Name,
            CreatedAt = check.CreatedAt,
            Status = check.Status,
        };
    }
}