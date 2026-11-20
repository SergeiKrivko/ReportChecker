using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportChecker.Exceptions;

namespace ReportChecker.SourceProviders.YandexDisk;

/// <summary>OAuth-клиент Яндекса (<c>oauth.yandex.ru</c>) и сведения о пользователе.</summary>
public class YandexDiskOAuthClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<YandexDiskOAuthClient> logger)
{
    /// <summary>Профиль пользователя Диска: отдаёт логин, если у приложения есть доступ к информации о Диске.</summary>
    public const string DiskInfoUrl = "https://cloud-api.yandex.net/v1/disk";

    public string ClientId { get; } = configuration["YandexDisk:ClientId"] ?? "";
    public string ClientSecret { get; } = configuration["YandexDisk:ClientSecret"] ?? "";
    public string RedirectUri { get; } = configuration["YandexDisk:RedirectUri"] ?? "";

    public string BuildAuthorizationUrl(string state)
    {
        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(RedirectUri))
            throw new InvalidOperationException("YandexDisk:ClientId и YandexDisk:RedirectUri не настроены");

        return "https://oauth.yandex.ru/authorize" +
               $"?response_type=code&client_id={Uri.EscapeDataString(ClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<YandexDiskTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var response = await PostTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
        }, ct);

        if (string.IsNullOrEmpty(response.AccessToken))
            throw new BadRequestException($"Яндекс OAuth не выдал токен: {response.ErrorDescription ?? response.Error}");
        return response;
    }

    public async Task<YandexDiskTokenResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var response = await PostTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            }, ct);
            return string.IsNullOrEmpty(response.AccessToken) ? null : response;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to refresh Yandex.Disk token");
            return null;
        }
    }

    public async Task<string?> TryGetLoginAsync(string accessToken, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, DiskInfoUrl);
            request.Headers.TryAddWithoutValidation("Authorization", $"OAuth {accessToken}");
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var info = System.Text.Json.JsonSerializer.Deserialize<DiskInfoDto>(json);
            return info?.User?.Login ?? info?.User?.DisplayName;
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Failed to get Yandex.Disk user info");
            return null;
        }
    }

    private async Task<YandexDiskTokenResponse> PostTokenAsync(Dictionary<string, string> values,
        CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(values);
        using var response = await httpClient.PostAsync("/token", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        YandexDiskTokenResponse? token;
        try
        {
            token = System.Text.Json.JsonSerializer.Deserialize<YandexDiskTokenResponse>(body);
        }
        catch (Exception)
        {
            token = null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Yandex OAuth {status}: {body}", (int)response.StatusCode, body);
            throw response.StatusCode == HttpStatusCode.BadRequest
                ? new BadRequestException($"Яндекс OAuth отклонил запрос: {token?.ErrorDescription ?? token?.Error}")
                : new InternalErrorException("Не удалось получить токен Яндекс.Диска");
        }

        return token ?? throw new InternalErrorException("Яндекс OAuth вернул пустой ответ");
    }

    public class YandexDiskTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("expires_in")] public long? ExpiresIn { get; init; }
        [JsonPropertyName("token_type")] public string? TokenType { get; init; }
        [JsonPropertyName("scope")] public string? Scope { get; init; }
        [JsonPropertyName("error")] public string? Error { get; init; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; init; }
    }

    private sealed class DiskInfoDto
    {
        [JsonPropertyName("user")] public DiskUserDto? User { get; init; }
    }

    private sealed class DiskUserDto
    {
        [JsonPropertyName("login")] public string? Login { get; init; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; init; }
    }
}
