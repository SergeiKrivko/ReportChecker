using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CreateReportShareDto
{
    public required string LoginOrEmail { get; init; }
    public ReportAccessLevel AccessLevel { get; init; } = ReportAccessLevel.Read;
}
