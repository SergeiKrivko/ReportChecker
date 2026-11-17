using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class UpdateReportShareDto
{
    public required ReportAccessLevel AccessLevel { get; init; }
}
