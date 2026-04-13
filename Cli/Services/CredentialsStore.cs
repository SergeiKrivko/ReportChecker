using Avalux.Auth.UserClient;
using Avalux.Auth.UserClient.Models;
using AvaluxUI.Utils;

namespace ReportChecker.Cli.Services;

internal class CredentialsStore(ISettingsSection settings) : ICredentialsStore
{
    private const string CredentialsKey = "credentials";

    public async Task SaveCredentials(UserCredentials? credentials, CancellationToken ct)
    {
        await settings.Set(CredentialsKey, credentials);
    }

    public async Task<UserCredentials?> LoadCredentials(CancellationToken ct)
    {
        return await settings.Get<UserCredentials>(CredentialsKey);
    }
}