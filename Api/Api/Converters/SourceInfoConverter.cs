using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class SourceInfoConverter
{
    public static SourceInfoDto ToDto(this SourceInfo sourceInfo)
    {
        return new SourceInfoDto
        {
            Status = sourceInfo.Status,
            Format = sourceInfo.Format,
        };
    }
}