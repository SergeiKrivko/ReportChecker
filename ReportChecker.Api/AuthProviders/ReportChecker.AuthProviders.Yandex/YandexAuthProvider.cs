using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.AuthProviders.Yandex;

public class YandexAuthProvider(IConfiguration configuration) : IAuthProvider
{
    private readonly HttpClient _httpClient = new();

    private string ClientId { get; } =
        configuration["YANDEX_CLIENT_ID"] ?? throw new Exception("Yandex client id not found");
    private string ClientSecret { get; } =
        configuration["YANDEX_CLIENT_SECRET"] ?? throw new Exception("Yandex client secret not found");

    public string Key => "Yandex";

    public Uri GetAuthorizationUrl(string redirectUrl)
    {
        return new Uri(
            $"https://oauth.yandex.ru/authorize" +
            $"?response_type=code" +
            $"&client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}");
    }

    private async Task<UserInfoResponse> GetUserInfo(string accessToken)
    {
        var resp = await _httpClient.SendAsync(new HttpRequestMessage
        {
            RequestUri = new Uri("https://login.yandex.ru/info"),
            Method = HttpMethod.Get,
            Headers = { Authorization = new AuthenticationHeaderValue($"Bearer {accessToken}") }
        });
        Console.WriteLine(await resp.Content.ReadAsStringAsync());
        var res = await resp.Content.ReadFromJsonAsync<UserInfoResponse>();
        return res ?? throw new Exception("Invalid response");
    }

    private class UserInfoResponse
    {
        [JsonPropertyName("psuid")] public required string Id { get; init; }
        [JsonPropertyName("login")] public required string Login { get; init; }
        [JsonPropertyName("default_email")] public required string DefaultEmail { get; init; }
        [JsonPropertyName("display_name")] public required string DisplayName { get; init; }
        [JsonPropertyName("real_name")] public required string RealName { get; init; }

        [JsonPropertyName("default_avatar_id")]
        public string? AvatarId { get; init; }
    }

    async Task<AuthorizedAccount> IAuthProvider.AuthorizeAsync(Dictionary<string, string> parameters,
        string? redirectUrl)
    {
        var queryParameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", parameters["code"] },
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "redirect_uri", redirectUrl ?? throw new Exception("Redirect URL is null") },
        };

        var response =
            await _httpClient.PostAsync("https://oauth.yandex.ru/token", new FormUrlEncodedContent(queryParameters));
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>() ??
                            throw new Exception("Invalid response");
        var userInfo = await GetUserInfo(tokenResponse.AccessToken);
        return new AuthorizedAccount
        {
            Credentials = JsonSerializer.Serialize(tokenResponse),
            Id = userInfo.Id,
        };
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public required string AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }
    }
}