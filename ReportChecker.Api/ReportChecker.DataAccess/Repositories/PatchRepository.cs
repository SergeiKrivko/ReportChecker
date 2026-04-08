using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class PatchRepository(ReportCheckerDbContext dbContext) : IPatchRepository
{
    public async Task<Patch?> GetPatchById(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.Patches
            .Where(p => p.Id == id)
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<Guid> CreatePatchAsync(Guid commentId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new PatchEntity
        {
            Id = id,
            CommentId = commentId,
            Status = PatchStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await dbContext.Patches.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdatePatchStatusAsync(Guid patchId, PatchStatus status, CancellationToken ct = default)
    {
        var count = await dbContext.Patches
            .Where(e => e.Id == patchId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, status), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task AddPatchLinesAsync(Guid patchId, IEnumerable<PatchLine> lines, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entities = lines.Select((e, i) => new PatchLineEntity
        {
            Id = Guid.NewGuid(),
            PatchId = patchId,
            Index = i,
            Number = e.Number,
            Content = e.Content,
            Type = e.Type,
            CreatedAt = now,
        });
        await dbContext.PatchLines.AddRangeAsync(entities, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}