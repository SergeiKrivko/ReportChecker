using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IPatchService
{
    public Task SetPatchStatus(Guid patchId, PatchStatus status, CancellationToken ct = default);
    public Task ApplyPatchAsync(Guid patchId, ICheckService checkService, CancellationToken ct = default);
}