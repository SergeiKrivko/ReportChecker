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
}