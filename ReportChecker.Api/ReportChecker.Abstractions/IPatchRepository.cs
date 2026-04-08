using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IPatchRepository
{
    public Task<Patch?> GetPatchById(Guid id, CancellationToken ct = default);
    public Task<Guid> CreatePatchAsync(Guid commentId, CancellationToken ct = default);
    public Task<bool> UpdatePatchStatusAsync(Guid patchId, PatchStatus status, CancellationToken ct = default);
    public Task AddPatchLinesAsync(Guid patchId, IEnumerable<PatchLine> lines, CancellationToken ct = default);
}