using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Converters;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class ReportShareRepository(ReportCheckerDbContext dbContext) : IReportShareRepository
{
    public async Task<IReadOnlyList<ReportShare>> GetSharesAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShares
            .AsNoTracking()
            .Where(e => e.ReportId == reportId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        return result.Select(e => e.ToDomain()).ToList();
    }

    public async Task<ReportShare?> GetShareAsync(Guid reportId, Guid userId, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShares
            .AsNoTracking()
            .Where(e => e.ReportId == reportId && e.UserId == userId)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<ReportShare> CreateShareAsync(Guid reportId, Guid userId,
        ReportAccessLevel accessLevel, CancellationToken ct = default)
    {
        var entity = new ReportShareEntity
        {
            ReportId = reportId,
            UserId = userId,
            AccessLevel = accessLevel,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
        };
        await dbContext.ReportShares.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);
        return entity.ToDomain();
    }

    public async Task<bool> UpdateShareAsync(Guid reportId, Guid userId,
        ReportAccessLevel accessLevel, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShares
            .Where(e => e.ReportId == reportId && e.UserId == userId)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.AccessLevel, accessLevel)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);
        return result > 0;
    }

    public async Task<bool> DeleteShareAsync(Guid reportId, Guid userId, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShares
            .Where(e => e.ReportId == reportId && e.UserId == userId)
            .ExecuteDeleteAsync(ct);
        return result > 0;
    }

    public async Task<ReportShareLink?> GetLinkAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShareLinks
            .AsNoTracking()
            .Where(e => e.ReportId == reportId)
            .FirstOrDefaultAsync(ct);
        return result?.ToDomain();
    }

    public async Task<ReportShareLink> CreateOrGetLinkAsync(Guid reportId, string token,
        CancellationToken ct = default)
    {
        var existing = await GetLinkAsync(reportId, ct);
        if (existing != null)
            return existing;

        var entity = new ReportShareLinkEntity
        {
            ReportId = reportId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
        };
        await dbContext.ReportShareLinks.AddAsync(entity, ct);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return entity.ToDomain();
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(entity).State = EntityState.Detached;
            return await GetLinkAsync(reportId, ct) ??
                   throw new InvalidOperationException("Report share link was not created");
        }
    }

    public async Task<ReportShareLink> ReplaceLinkAsync(Guid reportId, string token,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var updated = await dbContext.ReportShareLinks
            .Where(e => e.ReportId == reportId)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.Token, token)
                .SetProperty(x => x.CreatedAt, now), ct);
        if (updated > 0)
            return new ReportShareLink
            {
                ReportId = reportId,
                Token = token,
                CreatedAt = now,
            };

        return await CreateOrGetLinkAsync(reportId, token, ct);
    }

    public async Task<bool> DeleteLinkAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = await dbContext.ReportShareLinks
            .Where(e => e.ReportId == reportId)
            .ExecuteDeleteAsync(ct);
        return result > 0;
    }
}
