using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class InstructionTaskRepository(ReportCheckerDbContext dbContext) : IInstructionTaskRepository
{
    public async Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId)
    {
        var entities = await dbContext.InstructionTasks
            .Where(e => e.ReportId == reportId)
            .ToListAsync();
        return entities.Select(FromEntity).ToList();
    }

    public async Task<IReadOnlyList<InstructionTask>> GetAllForReportAsync(Guid reportId, ProgressStatus status)
    {
        var entities = await dbContext.InstructionTasks
            .Where(e => e.ReportId == reportId && e.Status == status)
            .ToListAsync();
        return entities.Select(FromEntity).ToList();
    }

    public async Task<Guid> CreateAsync(Guid reportId, string instruction, ProgressStatus status = ProgressStatus.Queued)
    {
        var id = Guid.NewGuid();
        var entity = new InstructionTaskEntity
        {
            Id = id,
            ReportId = reportId,
            Status = status,
            Instruction = instruction,
            CreatedAt = DateTime.UtcNow
        };
        await dbContext.InstructionTasks.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task<bool> SetStatusAsync(Guid taskId, ProgressStatus status)
    {
        var count = await dbContext.InstructionTasks
            .Where(e => e.Id == taskId)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.Status, status));
        await dbContext.SaveChangesAsync();
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
        };
    }
}