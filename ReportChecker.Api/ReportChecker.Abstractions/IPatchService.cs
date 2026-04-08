using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IPatchService
{
    public Task<Guid> CreatePatchAsync(Guid commentId, Chapter chapter, CancellationToken ct = default);
    public Task RunPatchAsync(Guid patchId, Chapter chapter, CancellationToken ct = default);
    public Task SetPatchStatus(Guid patchId, PatchStatus status, CancellationToken ct = default);
    public Task ApplyPatchAsync(Guid patchId, CancellationToken ct = default);
}