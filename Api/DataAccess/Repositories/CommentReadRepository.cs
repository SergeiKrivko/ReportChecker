using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Repositories;

public class CommentReadRepository(ReportCheckerDbContext dbContext) : ICommentReadRepository
{
    public async Task AddAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entities = commentIds.Select(e => new CommentReadEntity
        {
            CommentId = e,
            UserId = userId,
            CreatedAt = now,
        });
        await dbContext.CommentReads.AddRangeAsync(entities, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken ct = default)
    {
        var ids = commentIds.ToArray();
        if (ids.Length == 0)
            return;

        await dbContext.CommentReads
            .Where(e => e.UserId == userId && ids.Contains(e.CommentId))
            .ExecuteDeleteAsync(ct);
    }
}