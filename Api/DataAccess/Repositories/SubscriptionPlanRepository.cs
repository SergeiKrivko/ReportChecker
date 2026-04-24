using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class SubscriptionPlanRepository(ReportCheckerDbContext dbContext) : ISubscriptionPlanRepository
{
    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllPlansAsync(CancellationToken ct = default)
    {
        var entities = await dbContext.SubscriptionPlans
            .Where(e => e.DeletedAt == null)
            .Include(e => e.Offers)
            .OrderBy(e => e.TokensLimit)
            .ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await dbContext.SubscriptionPlans
            .Where(e => e.Id == id)
            .Include(e => e.Offers)
            .FirstOrDefaultAsync(ct);
        return entity?.ToDomain();
    }

    public async Task<Guid> CreatePlanAsync(string name, string? description, int tokenLimit, int reportsLimit,
        bool isHidden,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = new SubscriptionPlanEntity
        {
            Id = id,
            Name = name,
            Description = description,
            TokensLimit = tokenLimit,
            ReportsLimit = reportsLimit,
            IsHidden = isHidden,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        };
        await dbContext.SubscriptionPlans.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> UpdatePlanAsync(Guid id, string name, string? description, int tokenLimit, int reportsLimit,
        bool isHidden,
        CancellationToken ct = default)
    {
        var count = await dbContext.SubscriptionPlans
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(p => p
                    .SetProperty(e => e.Name, name)
                    .SetProperty(e => e.Description, description)
                    .SetProperty(e => e.TokensLimit, tokenLimit)
                    .SetProperty(e => e.ReportsLimit, reportsLimit)
                    .SetProperty(e => e.IsHidden, isHidden)
                , ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    public async Task<bool> DeletePlanAsync(Guid id, CancellationToken ct = default)
    {
        var count = await dbContext.SubscriptionPlans
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(p => p
                    .SetProperty(e => e.DeletedAt, DateTime.UtcNow)
                , ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }
}