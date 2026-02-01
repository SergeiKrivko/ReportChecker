using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAccountRepository
{
    public Task<IEnumerable<Account>> GetAccountsOfUserAsync(Guid userId);
    public Task<Account?> GetAccountByIdAsync(Guid accountId);
    public Task<Account?> GetAccountByProviderIdAsync(string id);
    public Task<Guid> CreateAccountAsync(Guid userId, string provider, string providerUserId);
    public Task DeleteAccountAsync(Guid accountId);
}