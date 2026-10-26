using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class UpdatePatchDto
{
    public required PatchStatus Status { get; init; }
}