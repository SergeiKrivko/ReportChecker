using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IUserRepository
{
    public Task<UserInfo?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    public Task<UserInfo?> GetUserByNameAsync(string name, string provider, CancellationToken ct = default);
}