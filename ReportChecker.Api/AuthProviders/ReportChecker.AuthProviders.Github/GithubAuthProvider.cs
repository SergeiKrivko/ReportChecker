using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Octokit;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.AuthProviders.Github;

public class GithubAuthProvider(IConfiguration configuration) : IAuthProvider
{
    private string ClientId { get; } =
        configuration["GITHUB_CLIENT_ID"] ?? throw new Exception("GITHUB_CLIENT_ID not set");

    private string ClientSecret { get; } =
        configuration["GITHUB_CLIENT_SECRET"] ?? throw new Exception("GITHUB_CLIENT_SECRET not set");

    private string AppName { get; } =
        configuration["GIT_HUB_APP_NAME"] ?? throw new Exception("GIT_HUB_APP_NAME not set");

    private readonly GitHubClient _client = new(new ProductHeaderValue(configuration["GIT_HUB_APP_NAME"]));

    public string Key => "Github";

    public Uri GetAuthorizationUrl(string redirectUrl)
    {
        return _client.Oauth.GetGitHubLoginUrl(new OauthLoginRequest(ClientId)
        {
            RedirectUri = new Uri(redirectUrl),
            Scopes = { "user", "user:email" }
        });
    }

    public async Task<JsonObject?> AuthorizeAsync(Dictionary<string, string> parameters, string? redirectUrl)
    {
        // Получаем access token
        var request = new OauthTokenRequest(ClientId, ClientSecret, parameters["code"]);
        var token = await _client.Oauth.CreateAccessToken(request);

        if (string.IsNullOrEmpty(token.AccessToken))
        {
            return RedirectToAction("Error");
        }

        // Создаем клиент с токеном пользователя
        var userClient = new GitHubClient(new ProductHeaderValue(_configuration["GitHub:AppName"]))
        {
            Credentials = new Credentials(token.AccessToken)
        };

        // Получаем информацию о пользователе
        var user = await userClient.User.Current();
        var emails = await userClient.User.Email.GetAll();

        // Аутентифицируем пользователя в вашем приложении
        await SignInUser(user, emails, token.AccessToken);

        var returnUrl = HttpContext.Session.GetString("GitHubReturnUrl") ?? "/";
        return LocalRedirect(returnUrl);
    }

    public async Task<AccountInfo?> GetAccountInfoAsync(ClaimsPrincipal user)
    {
        throw new NotImplementedException();
    }

    public bool VerifyProvider(ClaimsPrincipal user)
    {
        throw new NotImplementedException();
    }
}