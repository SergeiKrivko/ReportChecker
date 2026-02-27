using GitHubJwt;
using Microsoft.Extensions.Configuration;
using Octokit;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GithubService(IUserRepository userRepository, IConfiguration configuration)
{
    private readonly GitHubClient _client = new(new ProductHeaderValue(configuration["GitHub.AppName"]))
    {
        Credentials = new Credentials(GenerateJwt(), AuthenticationType.Bearer)
    };

    public async Task<GitHubClient> CreateUserClient(Guid userId)
    {
        var user = await userRepository.GetUserByIdAsync(userId);
        var account = user?.Accounts.FirstOrDefault(e => e.Provider == "github");
        if (account == null)
            throw new Exception("User credentials not found");

        var installation = await _client.GitHubApps.GetUserInstallationForCurrent(account.Name);
        var token = await _client.GitHubApps.CreateInstallationToken(installation.Id);

        return new GitHubClient(new ProductHeaderValue(configuration["GitHub.AppName"]))
        {
            Credentials = new Credentials(token.Token),
        };
    }

    public async Task<GitHubClient> CreateRepositoryClient(long repositoryId)
    {
        var installation = await _client.GitHubApps.GetRepositoryInstallationForCurrent(repositoryId);
        var token = await _client.GitHubApps.CreateInstallationToken(installation.Id);

        return new GitHubClient(new ProductHeaderValue(configuration["GitHub.AppName"]))
        {
            Credentials = new Credentials(token.Token),
        };
    }

    private static string GenerateJwt()
    {
        var privateKeySource = new EnvironmentVariablePrivateKeySource("GitHub.PrivateKey");
        var generator = new GitHubJwtFactory(
            privateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = 2793377,
                ExpirationSeconds = 600
            }
        );

        return generator.CreateEncodedJwtToken();
    }

    public async Task<IEnumerable<Models.Repository>> GetRepositories(Guid userId)
    {
        var client = await CreateUserClient(userId);

        var repositories = await client.Repository.GetAllForCurrent();
        return repositories.Select(e => new Models.Repository
        {
            Id = e.Id,
            Name = e.Name
        });
    }
}