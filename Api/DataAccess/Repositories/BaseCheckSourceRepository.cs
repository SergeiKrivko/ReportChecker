using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public abstract class BaseCheckSourceRepository<TSource, TEntity>(DbSet<TEntity> dbSet, DbContext dbContext)
    : ICheckSourceRepository<TSource> where TEntity : BaseCheckSourceEntity where TSource : class
{
    public async Task<CheckSource<TSource>?> GetByCheckIdAsync(Guid checkId, CancellationToken ct = default)
    {
        var entity = await dbSet.Where(e => e.CheckId == checkId).FirstOrDefaultAsync(ct);
        return entity == null
            ? null
            : new CheckSource<TSource>
            {
                Id = entity.Id,
                CheckId = entity.CheckId,
                Data = FromEntity(entity)
            };
    }

    public async Task<CheckSource<TSource>?> GetByIdAsync(Guid sourceId, CancellationToken ct = default)
    {
        var entity = await dbSet.Where(e => e.Id == sourceId).FirstOrDefaultAsync(ct);
        return entity == null
            ? null
            : new CheckSource<TSource>
            {
                Id = entity.Id,
                CheckId = entity.CheckId,
                Data = FromEntity(entity)
            };
    }

    public async Task<Guid> CreateAsync(Guid? checkId, TSource data, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = ToEntity(id, checkId, data);
        await dbSet.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<Guid> CreateAsync(Guid id, Guid? checkId, TSource data, CancellationToken ct = default)
    {
        var entity = ToEntity(id, checkId, data);
        await dbSet.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    public async Task<bool> AttachAsync(Guid sourceId, Guid checkId, CancellationToken ct = default)
    {
        var entity = await dbSet.Where(e => e.Id == sourceId).FirstOrDefaultAsync(ct);
        if (entity == null)
            return false;
        if (entity.CheckId != null && entity.CheckId != checkId)
            throw new Exception("Source is already attached");
        var count = await dbSet.Where(e => e.Id == sourceId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CheckId, checkId), ct);
        await dbContext.SaveChangesAsync(ct);
        return count > 0;
    }

    protected abstract TSource FromEntity(TEntity entity);
    protected abstract TEntity ToEntity(Guid id, Guid? checkId, TSource data);
}