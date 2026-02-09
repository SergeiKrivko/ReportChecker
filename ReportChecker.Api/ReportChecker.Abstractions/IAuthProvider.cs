using System.Security.Claims;
using System.Text.Json.Nodes;
using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IAuthProvider
{
    public string Key { get; }

    public Uri GetAuthorizationUrl(string redirectUrl);

    public Task<AuthorizedAccount> AuthorizeAsync(Dictionary<string, string> parameters, string? redirectUrl);
}