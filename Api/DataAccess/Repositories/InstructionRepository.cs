using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class InstructionRepository(ReportCheckerDbContext dbContext) : IInstructionRepository
{
    public async Task<Instruction?> GetInstructionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.Instructions
            .Where(i => i.Id == id && i.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<IEnumerable<Instruction>> GetInstructionsAsync(Guid reportId, CancellationToken ct = default)
    {
        var entities = await dbContext.Instructions
            .Where(i => i.ReportId == reportId && i.DeletedAt == null)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain());
    }

    public async Task<Guid> CreateInstructionAsync(Guid reportId, string content, Guid userId, Guid? commentId = null,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new InstructionEntity
        {
            Id = id,
            ReportId = reportId,
            UserId = userId,
            CommentId = commentId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.Instructions.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdateInstructionAsync(Guid id, string content, Guid userId, CancellationToken ct = default)
    {
        var count = await dbContext.Instructions
            .Where(i => i.Id == id && i.DeletedAt == null)
            .ExecuteUpdateAsync(p => p
                .SetProperty(e => e.Content, content)
                .SetProperty(e => e.UserId, userId), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<bool> DeleteInstructionAsync(Guid id, CancellationToken ct = default)
    {
        var count = await dbContext.Instructions
            .Where(i => i.Id == id && i.DeletedAt == null)
            .ExecuteUpdateAsync(i => i.SetProperty(e => e.DeletedAt, DateTime.UtcNow), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }
}