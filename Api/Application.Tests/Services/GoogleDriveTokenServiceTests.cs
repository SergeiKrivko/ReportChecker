using System.Net;
using Avalux.Auth.ApiClient;
using FluentAssertions;
using Moq;
using AccountCredentials = Avalux.Auth.ApiClient.Models.AccountCredentials;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class GoogleDriveTokenServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private Mock<IAuthClient> _authClient = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private GoogleDriveTokenService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _authClient = new Mock<IAuthClient>();
        _userRepository = new Mock<IUserRepository>();
        _service = new GoogleDriveTokenService(_authClient.Object, _userRepository.Object);
    }

    [Test]
    public async Task GetAccessToken_ReturnsTokenFromAuthService()
    {
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCredentials
            {
                AccessToken = "go-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            });

        (await _service.GetAccessTokenAsync(UserId)).Should().Be("go-token");
    }

    [Test]
    public async Task GetAccessToken_NoCredentials_ThrowsUnauthorized()
    {
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("not found", null, HttpStatusCode.NotFound));

        var act = () => _service.GetAccessTokenAsync(UserId);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Test]
    public async Task GetAccessToken_EmptyToken_ThrowsUnauthorized()
    {
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCredentials { AccessToken = "", ExpiresAt = DateTime.UtcNow });

        var act = () => _service.GetAccessTokenAsync(UserId);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Test]
    public async Task GetConnection_NoGoogleAccount_NotConnected()
    {
        _userRepository
            .Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo
            {
                Id = UserId,
                Accounts =
                [
                    new AccountInfo { Id = "1", Provider = "github", Login = "octocat" },
                ],
            });

        var connection = await _service.GetConnectionAsync(UserId);

        connection.HasAccount.Should().BeFalse();
        connection.Connected.Should().BeFalse();
        _authClient.Verify(
            e => e.GetAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetConnection_AccountWithoutToken_ReportsAccountButNotConnected()
    {
        _userRepository
            .Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo
            {
                Id = UserId,
                Accounts =
                [
                    new AccountInfo { Id = "2", Provider = "google", Login = "google-user" },
                ],
            });
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("not found", null, HttpStatusCode.NotFound));

        var connection = await _service.GetConnectionAsync(UserId);

        connection.HasAccount.Should().BeTrue();
        connection.Connected.Should().BeFalse();
        connection.Login.Should().Be("google-user");
    }

    [Test]
    public async Task GetConnection_AccountWithToken_Connected()
    {
        _userRepository
            .Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo
            {
                Id = UserId,
                Accounts =
                [
                    new AccountInfo { Id = "2", Provider = "google", Login = "google-user" },
                ],
            });
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCredentials
            {
                AccessToken = "go-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            });

        var connection = await _service.GetConnectionAsync(UserId);

        connection.HasAccount.Should().BeTrue();
        connection.Connected.Should().BeTrue();
        connection.Login.Should().Be("google-user");
    }

    [Test]
    public async Task GetConnection_AuthServiceUnavailable_Propagates()
    {
        _userRepository
            .Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo
            {
                Id = UserId,
                Accounts = [new AccountInfo { Id = "2", Provider = "google", Login = "google-user" }],
            });
        _authClient
            .Setup(e => e.GetAccessTokenAsync(UserId, "google", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("boom", null, HttpStatusCode.InternalServerError));

        var act = () => _service.GetConnectionAsync(UserId);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
