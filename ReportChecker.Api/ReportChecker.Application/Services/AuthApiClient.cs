using System.Net.Http.Json;
using System.Text.Json;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class AuthApiClient(IHttpClientFactory httpClientFactory) : IUserRepository
{
    private readonly HttpClient _client = httpClientFactory.CreateClient("Auth");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<UserInfo?> GetUserByNameAsync(string name, string provider, CancellationToken ct = default)
    {
        var response =
            await _client.GetAsync($"api/v1/service/users?search={Uri.EscapeDataString(name)}&provider={provider}", ct);
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<UserInfo[]>(_jsonOptions, ct) ?? [];
        return users.FirstOrDefault(e => e.Accounts[0].Name == name);
    }

    public async Task<UserInfo?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response =
            await _client.GetAsync($"api/v1/service/users/{id}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions, ct);
    }
}