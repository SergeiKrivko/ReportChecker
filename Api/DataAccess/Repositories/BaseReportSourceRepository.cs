using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.DataAccess.Repositories;

public abstract class BaseReportSourceRepository<TSource, TEntity, TExternalId>(
    DbSet<TEntity> dbSet,
    DbContext dbContext,
    Func<TExternalId, Expression<Func<TEntity, bool>>> externalIdSelector)
    : IReportSourceRepository<TSource> where TEntity : BaseReportSourceEntity where TSource : class
{
    public async Task<ReportSource<TSource>?> GetByReportIdAsync(Guid reportId, CancellationToken ct = default)
    {
        var entity = await dbSet.Where(e => e.ReportId == reportId).FirstOrDefaultAsync(ct);
        return entity == null
            ? null
            : new ReportSource<TSource>
            {
                Id = entity.Id,
                ReportId = entity.ReportId,
                Data = FromEntity(entity)
            };
    }

    public async Task<ReportSource<TSource>?> GetByIdAsync(Guid sourceId, CancellationToken ct = default)
    {
        var entity = await dbSet.Where(e => e.Id == sourceId).FirstOrDefaultAsync(ct);
        return entity == null
            ? null
            : new ReportSource<TSource>
            {
                Id = entity.Id,
                ReportId = entity.ReportId,
                Data = FromEntity(entity)
            };
    }

    public async Task<IReadOnlyList<ReportSource<TSource>>> GetByExternalIdAsync<T>(T tId,
        CancellationToken ct = default)
    {
        if (tId is not TExternalId externalId)
            throw new ArgumentException($"{nameof(tId)} must be of type {typeof(TExternalId)}");
        var entities = await dbSet.Where(externalIdSelector(externalId)).ToListAsync(ct);
        return entities.Select(entity => new ReportSource<TSource>
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Data = FromEntity(entity)
        }).ToList();
    }

    public async Task<Guid> CreateAsync(Guid reportId, TSource data, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var entity = ToEntity(id, reportId, data);
        await dbSet.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return id;
    }

    protected abstract TSource FromEntity(TEntity entity);
    protected abstract TEntity ToEntity(Guid id, Guid reportId, TSource data);
}