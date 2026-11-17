using System.Net;
using Avalux.Auth.ApiClient;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class AvaluxAuthUserRepository(IAuthClient authClient) : IUserRepository
{
    public async Task<UserInfo?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var resp = await authClient.GetUserAsync(id, ct);
            return FromResponse(resp);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserInfo?> GetUserByNameAsync(string name, string provider, CancellationToken ct = default)
    {
        var resp = await authClient.SearchUsersAsync(login: name, ct: ct);
        var found = resp.SingleOrDefault(e => e.Accounts.Select(a => a.Login).Contains(name));
        return found == null ? null : FromResponse(found);
    }

    public async Task<IReadOnlyList<UserInfo>> FindUsersByLoginOrEmailAsync(string loginOrEmail,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(loginOrEmail))
            return [];
        var query = loginOrEmail.Trim();

        var byLogin = await authClient.SearchUsersAsync(login: query, ct: ct);
        var byEmail = await authClient.SearchUsersAsync(email: query, ct: ct);

        return byLogin.Concat(byEmail)
            .Where(e => e.Accounts.Any(a =>
                string.Equals(a.Login, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.Email, query, StringComparison.OrdinalIgnoreCase)))
            .GroupBy(e => e.Id)
            .Select(e => FromResponse(e.First()))
            .ToList();
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