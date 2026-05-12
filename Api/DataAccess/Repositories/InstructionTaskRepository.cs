using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class InstructionTaskRepository(ReportCheckerDbContext dbContext) : IInstructionTaskRepository
{
    public async Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId,
        CancellationToken ct = default)
    {
        var entities = await dbContext.InstructionTasks
            .Where(e => e.ReportId == reportId)
            .ToListAsync(ct);
        return entities.Select(FromEntity).ToList();
    }

    public async Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId, ProgressStatus status,
        CancellationToken ct = default)
    {
        var entities = await dbContext.InstructionTasks
            .Where(e => e.ReportId == reportId && e.Status == status)
            .ToListAsync(ct);
        return entities.Select(FromEntity).ToList();
    }

    public async Task<InstructionTask?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var entity = await dbContext.InstructionTasks
            .Where(e => e.Id == taskId)
            .FirstOrDefaultAsync(ct);
        return entity == null ? null : FromEntity(entity);
    }

    public async Task<Guid> CreateAsync(Guid reportId, string instruction, InstructionTaskMode mode,
        ProgressStatus status = ProgressStatus.Queued, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new InstructionTaskEntity
        {
            Id = id,
            ReportId = reportId,
            Status = status,
            Mode = mode,
            Instruction = instruction,
            CreatedAt = DateTime.UtcNow
        };
        await dbContext.InstructionTasks.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> SetStatusAsync(Guid taskId, ProgressStatus status, CancellationToken ct = default)
    {
        var count = await dbContext.InstructionTasks
            .Where(e => e.Id == taskId)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Status, status), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    private static InstructionTask FromEntity(InstructionTaskEntity entity)
    {
        return new InstructionTask
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Status = entity.Status,
            Instruction = entity.Instruction,
            Mode = entity.Mode,
        };
    }
}