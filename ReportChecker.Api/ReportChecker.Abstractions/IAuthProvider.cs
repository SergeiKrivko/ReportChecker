using System.Security.Claims;
using System.Text.Json.Nodes;
using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAuthProvider
{
    public string Key { get; }

    public Task<JsonObject?> AuthorizeAsync(Dictionary<string, string> parameters, string? redirectUrl);

    public Task<AccountInfo?> GetAccountInfoAsync(ClaimsPrincipal user);

    public bool VerifyProvider(ClaimsPrincipal user);
}