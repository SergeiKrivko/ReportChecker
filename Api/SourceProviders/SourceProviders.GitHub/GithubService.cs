using GitHubJwt;
using Microsoft.Extensions.Configuration;
using Octokit;
using ReportChecker.Abstractions;
using ReportChecker.SourceProviders.GitHub.Models;
using StackExchange.Redis;

namespace ReportChecker.SourceProviders.GitHub;

public class GithubService(
    IUserRepository userRepository,
    IConfiguration configuration,
    IConnectionMultiplexer redisConnection)
{
    private readonly IDatabase _redis = redisConnection.GetDatabase(2);

    private readonly GitHubClient _client = new(new ProductHeaderValue(configuration["GitHub.AppName"]))
    {
        Credentials = new Credentials(GenerateJwt(), AuthenticationType.Bearer),
    };

    private async Task<GitHubClient> CreateUserClient(Guid userId)
    {
        var user = await userRepository.GetUserByIdAsync(userId);
        var account = user?.Accounts.FirstOrDefault(e => e.Provider == "github");
        if (account == null)
            throw new Exception("User credentials not found");

        var installation = await _client.GitHubApps.GetUserInstallationForCurrent(account.Login ?? account.Name);
        var token = await GetInstallationTokenAsync(installation.Id);

        return new GitHubClient(new ProductHeaderValue(configuration["GitHub.AppName"]))
        {
            Credentials = new Credentials(token),
        };
    }

    public async Task<GitHubClient> CreateRepositoryClient(long repositoryId)
    {
        var installation = await _client.GitHubApps.GetRepositoryInstallationForCurrent(repositoryId);
        var token = await GetInstallationTokenAsync(installation.Id);

        return new GitHubClient(new ProductHeaderValue(configuration["GitHub.AppName"]))
        {
            Credentials = new Credentials(token),
        };
    }

    private async Task<string> GetInstallationTokenAsync(long installationId)
    {
        var key = $"installation-token-{installationId}";
        var cache = await _redis.StringGetAsync(key);
        if (cache.HasValue)
            return cache.ToString();
        var token = await _client.GitHubApps.CreateInstallationToken(installationId);
        var ttl = token.ExpiresAt.AddMinutes(-5) - DateTime.UtcNow;
        await _redis.StringSetAsync(key, token.Token, ttl);
        return token.Token;
    }

    private static string? _jwt;
    private static DateTime _jwtExpiresAt = DateTime.UnixEpoch;

    private static string GenerateJwt()
    {
        var now = DateTime.UtcNow;
        if (_jwtExpiresAt > now && _jwt != null)
            return _jwt;

        var privateKeySource = new EnvironmentVariablePrivateKeySource("GitHub.PrivateKey");
        var generator = new GitHubJwtFactory(
            privateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = 2793377,
                ExpirationSeconds = 590
            }
        );

        _jwt = generator.CreateEncodedJwtToken();
        _jwtExpiresAt = now;
        return _jwt;
    }

    public async Task<IEnumerable<Models.Repository>> GetRepositories(Guid userId)
    {
        var client = await CreateUserClient(userId);

        var repositories = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();
        return repositories.Repositories.Select(e => new Models.Repository
        {
            Id = e.Id,
            Name = e.Name
        });
    }

    public async Task<string[]> GetBranchesOfRepositoryAsync(Guid userId, long repositoryId)
    {
        var client = await CreateUserClient(userId);
        var branches = await client.Repository.Branch.GetAll(repositoryId);
        return branches.Select(e => e.Name).ToArray();
    }

    public async Task<RepositoryFile[]> GetFilesOfRepositoryAsync(Guid userId, long repositoryId, string branchName)
    {
        var client = await CreateUserClient(userId);
        var branch = await client.Repository.Branch.Get(repositoryId, branchName);
        var contents =
            await client.Repository.Content.GetAllContents(repositoryId, branch.Commit.Sha);
        return contents.Select(e => new RepositoryFile
        {
            Name = e.Name,
            Path = e.Path,
        }).ToArray();
    }

    public async Task<RepositoryInfo> GetRepositoryInfoAsync(Guid userId, long repositoryId)
    {
        var client = await CreateUserClient(userId);
        var repository = await client.Repository.Get(repositoryId);
        return new RepositoryInfo
        {
            Id = repositoryId,
            Name = repository.Name,
            Url = repository.HtmlUrl,
        };
    }

    public async Task<bool> CheckInstallation(Guid userId)
    {
        var user = await userRepository.GetUserByIdAsync(userId);
        var account = user?.Accounts.FirstOrDefault(e => e.Provider == "github");
        if (account == null)
            return false;

        try
        {
            await _client.GitHubApps.GetUserInstallationForCurrent(account.Login ?? account.Name);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}