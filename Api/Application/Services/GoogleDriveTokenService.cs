using System.Net;
using Avalux.Auth.ApiClient;
using Avalux.Auth.ApiClient.Models;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models.GoogleDrive;

namespace ReportChecker.Application.Services;

/// <summary>
/// Токен Google Диска берётся из Avalux Auth: там же хранится привязанный
/// Google-аккаунт, и сервис авторизации сам обновляет токен.
/// </summary>
public class GoogleDriveTokenService(IAuthClient authClient, IUserRepository userRepository)
    : IGoogleDriveTokenService
{
    /// <summary>Идентификатор провайдера в Avalux Auth.</summary>
    public const string Provider = "google";

    private const string NotConnectedMessage =
        "Нет доступа к Google Диску. Привяжите Google-аккаунт на странице «Аккаунт» и войдите через Google";

    public async Task<GoogleDriveConnection> GetConnectionAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetUserByIdAsync(userId, ct);
        var account = user?.Accounts.FirstOrDefault(e =>
            string.Equals(e.Provider, Provider, StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return new GoogleDriveConnection { HasAccount = false, Connected = false };

        var credentials = await TryGetCredentialsAsync(userId, ct);
        return new GoogleDriveConnection
        {
            HasAccount = true,
            Connected = credentials != null,
            Login = account.Login ?? account.Name,
        };
    }

    public async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var credentials = await TryGetCredentialsAsync(userId, ct);
        if (string.IsNullOrEmpty(credentials?.AccessToken))
            throw new UnauthorizedException(NotConnectedMessage);

        return credentials.AccessToken;
    }

    private async Task<AccountCredentials?> TryGetCredentialsAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var credentials = await authClient.GetAccessTokenAsync(userId, Provider, ct);
            return string.IsNullOrEmpty(credentials?.AccessToken) ? null : credentials;
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound
                                                 or HttpStatusCode.Unauthorized
                                                 or HttpStatusCode.Forbidden
                                                 or HttpStatusCode.BadRequest)
        {
            // Аккаунт не привязан или токен не выдан — для вызывающего это одно и то же.
            return null;
        }
    }
}
