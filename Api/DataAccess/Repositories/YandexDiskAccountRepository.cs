using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Exceptions;
using ReportChecker.Models.YandexDisk;

namespace ReportChecker.DataAccess.Repositories;

public class YandexDiskAccountRepository(
    ReportCheckerDbContext dbContext,
    IYandexDiskTokenProtector tokenProtector) : IYandexDiskAccountRepository
{
    public async Task<YandexDiskAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var entity = await dbContext.YandexDiskAccounts
            .FirstOrDefaultAsync(e => e.UserId == userId, ct);
        if (entity == null)
            return null;

        try
        {
            return new YandexDiskAccount
            {
                UserId = entity.UserId,
                Login = entity.Login,
                AccessToken = tokenProtector.Unprotect(entity.AccessToken),
                RefreshToken = entity.RefreshToken == null ? null : tokenProtector.Unprotect(entity.RefreshToken),
                ExpiresAt = entity.ExpiresAt,
            };
        }
        catch (Exception e) when (e is not ReportCheckerBaseException)
        {
            throw new InternalErrorException(
                "Не удалось расшифровать токен Яндекс.Диска. Проверьте, что YandexDisk:TokenEncryptionKey не менялся",
                e);
        }
    }

    public async Task SaveAsync(YandexDiskAccount account, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var accessToken = tokenProtector.Protect(account.AccessToken);
        var refreshToken = account.RefreshToken == null ? null : tokenProtector.Protect(account.RefreshToken);

        var entity = await dbContext.YandexDiskAccounts
            .FirstOrDefaultAsync(e => e.UserId == account.UserId, ct);
        if (entity == null)
        {
            await dbContext.YandexDiskAccounts.AddAsync(new YandexDiskAccountEntity
            {
                UserId = account.UserId,
                Login = account.Login,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = account.ExpiresAt,
                CreatedAt = now,
                UpdatedAt = now,
            }, ct);
        }
        else
        {
            await dbContext.YandexDiskAccounts
                .Where(e => e.UserId == account.UserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Login, account.Login)
                    .SetProperty(e => e.AccessToken, accessToken)
                    .SetProperty(e => e.RefreshToken, refreshToken)
                    .SetProperty(e => e.ExpiresAt, account.ExpiresAt)
                    .SetProperty(e => e.UpdatedAt, now), ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        await dbContext.YandexDiskAccounts
            .Where(e => e.UserId == userId)
            .ExecuteDeleteAsync(ct);
        await dbContext.SaveChangesAsync(ct);
    }
}
