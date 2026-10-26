using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class SourceInfoDto
{
    public required SourceStatus Status { get; init; }
    public string? Format { get; init; }
}