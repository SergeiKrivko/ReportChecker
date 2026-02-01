using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Repositories;

public class AccountRepository(ReportCheckerDbContext dbContext) : IAccountRepository
{
    public async Task<IEnumerable<Account>> GetAccountsOfUserAsync(Guid userId)
    {
        var result = await dbContext.Accounts
            .Where(e => e.UserId == userId && e.DeletedAt == null)
            .ToListAsync();
        return result.Select(FromEntity);
    }

    public async Task<Account?> GetAccountByIdAsync(Guid accountId)
    {
        var result = await dbContext.Accounts
            .Where(e => e.AccountId == accountId && e.DeletedAt == null)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<Account?> GetAccountByProviderIdAsync(string id)
    {
        var result = await dbContext.Accounts
            .Where(e => e.ProviderUserId == id && e.DeletedAt == null)
            .FirstOrDefaultAsync();
        return result is null ? null : FromEntity(result);
    }

    public async Task<Guid> CreateAccountAsync(Guid userId, string provider, string providerUserId)
    {
        var id = Guid.NewGuid();
        var entity = new AccountEntity
        {
            AccountId = id,
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        await dbContext.Accounts.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return id;
    }

    public async Task DeleteAccountAsync(Guid accountId)
    {
        await dbContext.Accounts
            .Where(e => e.AccountId == accountId && e.DeletedAt == null)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.DeletedAt, DateTime.UtcNow));
    }

    private static Account FromEntity(AccountEntity entity)
    {
        return new Account
        {
            Id = entity.AccountId,
            UserId = entity.UserId,
            Provider = entity.Provider,
            ProviderUserId = entity.ProviderUserId,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}