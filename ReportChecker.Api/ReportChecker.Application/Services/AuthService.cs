using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class AuthService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Auth");

    private string ClientId => configuration["Security.ClientId"] ??
                               throw new InvalidOperationException("Security.ClientId is required");

    private string ClientSecret => configuration["Security.ClientSecret"] ??
                                   throw new InvalidOperationException("Security.ClientSecret is required");

    private string ApiUrl => configuration["Security.AuthApiUrl"] ??
                             throw new InvalidOperationException("Security.AuthApiUrl is required");

    public string GetAuthUrl(string provider, string redirectUrl)
    {
        return
            $"{ApiUrl}/api/v1/auth/{provider}/authorize?redirect_uri={redirectUrl}&client_id={ClientId}&response_type=code";
    }

    public async Task<UserCredentials> GetTokenAsync(string code)
    {
        var resp = await _httpClient.PostAsync("api/v1/auth/token", new FormUrlEncodedContent(
            new Dictionary<string, string>()
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code", code },
            }));
        resp.EnsureSuccessStatusCode();
        var token = await resp.Content.ReadFromJsonAsync<UserCredentials>();
        return token ?? throw new Exception("Invalid token");
    }

    public async Task<UserCredentials> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}