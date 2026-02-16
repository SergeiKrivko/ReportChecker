using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class InstructionRepository(ReportCheckerDbContext dbContext) : IInstructionRepository
{
    public async Task<Instruction?> GetInstructionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.Instructions
            .Where(i => i.Id == id && i.DeletedAt != null)
            .FirstOrDefaultAsync(ct);
        return entity is null ? null : FromEntity(entity);
    }

    public async Task<IEnumerable<Instruction>> GetInstructionsAsync(Guid reportId, CancellationToken ct = default)
    {
        var entities = await dbContext.Instructions
            .Where(i => i.ReportId == reportId && i.DeletedAt == null)
            .ToListAsync(ct);
        return entities.Select(FromEntity);
    }

    public async Task<Guid> CreateInstructionAsync(Guid reportId, string content, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new InstructionEntity
        {
            Id = id,
            ReportId = reportId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.Instructions.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdateInstructionAsync(Guid id, string content, CancellationToken ct = default)
    {
        var count = await dbContext.Instructions
            .Where(i => i.Id == id && i.DeletedAt == null)
            .ExecuteUpdateAsync(i => i.SetProperty(e => e.Content, content), ct);
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

    private static Instruction FromEntity(InstructionEntity entity)
    {
        return new Instruction
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}