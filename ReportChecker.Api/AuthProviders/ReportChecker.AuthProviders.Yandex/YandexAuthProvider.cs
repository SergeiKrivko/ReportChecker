using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.AuthProviders.Yandex;

public class YandexAuthProvider : IAuthProvider
{
    private readonly HttpClient _httpClient = new();

    public string Key => "Yandex";

    public async Task<JsonObject?> AuthorizeAsync(Dictionary<string, string> parameters, string? redirectUrl)
    {
        var queryParameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", parameters["code"] },
            { "client_id", Environment.GetEnvironmentVariable("YANDEX_CLIENT_ID") ?? throw new Exception("Yandex credentials not found") },
            { "client_secret", Environment.GetEnvironmentVariable("YANDEX_CLIENT_SECRET") ?? throw new Exception("Yandex credentials not found") },
            { "redirect_uri", redirectUrl ?? throw new Exception("Redirect URL is null") },
        };

        var response = await _httpClient.PostAsync("https://oauth.yandex.ru/token", new FormUrlEncodedContent(queryParameters));
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
        return tokenResponse;
    }

    public async Task<AccountInfo?> GetAccountInfoAsync(ClaimsPrincipal user)
    {
        return new AccountInfo
        {
            Id = user.Claims.FirstOrDefault(c => c.Type == "psuid")?.Value ?? throw new Exception("User id not found"),
            Username = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            Email = user.Claims.FirstOrDefault(c => c.Type == "login")?.Value + "@yandex.ru",
        };
    }

    public bool VerifyProvider(ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == "iss")?.Value == "login.yandex.ru";
    }
}