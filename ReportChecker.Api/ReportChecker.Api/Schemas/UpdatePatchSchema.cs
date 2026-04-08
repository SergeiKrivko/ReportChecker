using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class UpdatePatchSchema
{
    public required PatchStatus Status { get; init; }
}