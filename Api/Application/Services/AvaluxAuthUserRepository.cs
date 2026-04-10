using Avalux.Auth.ApiClient;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class AvaluxAuthUserRepository(IAuthClient authClient) : IUserRepository
{
    public async Task<UserInfo?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await authClient.GetUserAsync(id, ct);
        return FromResponse(resp);
    }

    public async Task<UserInfo?> GetUserByNameAsync(string name, string provider, CancellationToken ct = default)
    {
        var resp = await authClient.SearchUsersAsync(login: name, ct: ct);
        var found = resp.SingleOrDefault(e => e.Accounts.Select(a => a.Login).Contains(name));
        return found == null ? null : FromResponse(found);
    }

    private static UserInfo FromResponse(Avalux.Auth.ApiClient.Models.UserInfo response)
    {
        return new UserInfo
        {
            Id = response.Id,
            Accounts = response.Accounts.Select(e => new AccountInfo
            {
                Provider = e.Provider,
                Id = e.Id,
                Login = e.Login,
                Name = e.Name,
                Email = e.Email,
                AvatarUrl = e.AvatarUrl,
            }).ToArray(),
        };
    }
}