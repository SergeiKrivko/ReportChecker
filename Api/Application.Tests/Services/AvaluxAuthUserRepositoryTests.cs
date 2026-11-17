using System.Net;
using Avalux.Auth.ApiClient;
using FluentAssertions;
using Moq;
using ReportChecker.Application.Services;
using AuthModels = Avalux.Auth.ApiClient.Models;

namespace Application.Tests.Services;

[TestFixture]
public class AvaluxAuthUserRepositoryTests
{
    private Mock<IAuthClient> _authClient = null!;
    private AvaluxAuthUserRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _authClient = new Mock<IAuthClient>();
        _repository = new AvaluxAuthUserRepository(_authClient.Object);
    }

    [Test]
    public async Task FindUsers_ExactLogin_IsCaseInsensitive()
    {
        var user = CreateUser("Ivan", "ivan@example.com");
        SetupSearch("ivan", [user], []);

        var result = await _repository.FindUsersByLoginOrEmailAsync("ivan");

        result.Should().ContainSingle();
        result[0].Id.Should().Be(user.Id);
        result[0].Accounts[0].Login.Should().Be("Ivan");
    }

    [Test]
    public async Task FindUsers_ExactEmail_ReturnsUser()
    {
        var user = CreateUser("ivan", "Ivan@Example.com");
        SetupSearch("ivan@example.com", [], [user]);

        var result = await _repository.FindUsersByLoginOrEmailAsync("ivan@example.com");

        result.Should().ContainSingle();
        result[0].Id.Should().Be(user.Id);
    }

    [Test]
    public async Task FindUsers_SameUserInBothSearches_IsDeduplicated()
    {
        var user = CreateUser("ivan", "ivan@example.com");
        SetupSearch("ivan", [user], [user]);

        var result = await _repository.FindUsersByLoginOrEmailAsync("ivan");

        result.Should().ContainSingle();
    }

    [Test]
    public async Task FindUsers_NonExactMatch_IsIgnored()
    {
        var user = CreateUser("ivan.petrov", "ivan.petrov@example.com");
        SetupSearch("ivan", [user], []);

        var result = await _repository.FindUsersByLoginOrEmailAsync("ivan");

        result.Should().BeEmpty();
    }

    [Test]
    public async Task FindUsers_EmptyQuery_ReturnsEmptyWithoutAuthCalls()
    {
        var result = await _repository.FindUsersByLoginOrEmailAsync("   ");

        result.Should().BeEmpty();
        _authClient.Verify(
            e => e.SearchUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetUserById_ExistingUser_ReturnsMappedUser()
    {
        var user = CreateUser("ivan", "ivan@example.com");
        _authClient.Setup(e => e.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _repository.GetUserByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Accounts.Should().ContainSingle();
        result.Accounts[0].AvatarUrl.Should().Be(user.Accounts[0].AvatarUrl);
    }

    [Test]
    public async Task GetUserById_MissingUser_ReturnsNull()
    {
        _authClient.Setup(e => e.GetUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

        var result = await _repository.GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetUserById_AuthServiceError_IsPropagated()
    {
        _authClient.Setup(e => e.GetUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Error", null, HttpStatusCode.InternalServerError));

        var act = () => _repository.GetUserByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private void SetupSearch(string query, AuthModels.UserInfo[] byLogin, AuthModels.UserInfo[] byEmail)
    {
        _authClient.Setup(e => e.SearchUsersAsync(It.IsAny<int>(), It.IsAny<int>(), query, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(byLogin);
        _authClient.Setup(e => e.SearchUsersAsync(It.IsAny<int>(), It.IsAny<int>(), null, query, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(byEmail);
    }

    private static AuthModels.UserInfo CreateUser(string login, string email) => new()
    {
        Id = Guid.NewGuid(),
        Accounts =
        [
            new AuthModels.AccountInfo
            {
                Provider = "GitHub",
                Id = Guid.NewGuid().ToString(),
                Login = login,
                Email = email,
                Name = "Ivan",
                AvatarUrl = "https://example.com/avatar.png",
            },
        ],
    };
}
