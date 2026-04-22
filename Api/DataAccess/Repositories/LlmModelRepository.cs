using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class LlmModelRepository(ReportCheckerDbContext dbContext, IConfiguration configuration) : ILlmModelRepository
{
    private Guid DefaultModelId { get; } = Guid.Parse(configuration["Ai.DefaultModelId"] ?? "");

    public async Task<IReadOnlyList<LlmModel>> GetAllModelsAsync(CancellationToken ct = default)
    {
        var entities = await dbContext.LlmModels
            .Where(e => e.DeletedAt == null)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain(DefaultModelId)).ToList();
    }

    public async Task<LlmModel?> GetModelByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.LlmModels
            .Where(e => e.Id == id && e.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain(DefaultModelId);
    }

    public async Task<LlmModel> GetDefaultModelAsync(CancellationToken ct = default)
    {
        var entity = await dbContext.LlmModels
            .Where(e => e.Id == DefaultModelId && e.DeletedAt == null)
            .FirstAsync(ct);
        return entity.ToDomain(DefaultModelId);
    }

    public async Task<Guid> CreateModelAsync(string displayName, string modelKey, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new LlmModelEntity
        {
            Id = id,
            DisplayName = displayName,
            ModelKey = modelKey,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.LlmModels.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> DeleteModelAsync(Guid id, CancellationToken ct = default)
    {
        var count = await dbContext.LlmModels
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(e => e.DeletedAt, DateTime.UtcNow), ct);
        return count > 0;
    }
}