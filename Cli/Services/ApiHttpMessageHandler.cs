using System.Net.Http.Headers;
using Avalux.Auth.UserClient;

namespace ReportChecker.Cli.Services;

public class ApiHttpMessageHandler(IAuthClient authClient) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await authClient.RefreshTokenAsync(ct: cancellationToken);
        var accessToken = authClient.AccessToken;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}