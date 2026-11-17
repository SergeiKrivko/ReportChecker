using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class ReportShareServiceTests
{
    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private Mock<IReportShareRepository> _shareRepository = null!;
    private Mock<IReportAccessService> _accessService = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private ReportShareService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _shareRepository = new Mock<IReportShareRepository>();
        _accessService = new Mock<IReportAccessService>();
        _userRepository = new Mock<IUserRepository>();

        _accessService
            .Setup(e => e.RequireOwnerAsync(ReportId, OwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportAccess
            {
                Report = CreateReport(),
                Level = ReportAccessLevel.Owner,
            });

        _service = new ReportShareService(_shareRepository.Object, _accessService.Object,
            _userRepository.Object, NullLogger<ReportShareService>.Instance);
    }

    [Test]
    public async Task ShareWithUser_ByLogin_CreatesReadShare()
    {
        var user = CreateUser("ivan", "ivan@example.com");
        _userRepository.Setup(e => e.FindUsersByLoginOrEmailAsync("ivan", It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _shareRepository
            .Setup(e => e.CreateShareAsync(ReportId, UserId, ReportAccessLevel.Read,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShare(ReportAccessLevel.Read));

        var result = await _service.ShareWithUserAsync(ReportId, OwnerId, "ivan", ReportAccessLevel.Read);

        result.User.Should().Be(user);
        result.Share.AccessLevel.Should().Be(ReportAccessLevel.Read);
        _shareRepository.Verify(e => e.GetShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ShareWithUser_ByEmail_PassesQueryToRepository()
    {
        var user = CreateUser("ivan", "ivan@example.com");
        _userRepository
            .Setup(e => e.FindUsersByLoginOrEmailAsync("ivan@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _shareRepository
            .Setup(e => e.CreateShareAsync(ReportId, UserId, ReportAccessLevel.Comment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShare(ReportAccessLevel.Comment));

        var result = await _service.ShareWithUserAsync(ReportId, OwnerId, "ivan@example.com",
            ReportAccessLevel.Comment);

        result.Share.AccessLevel.Should().Be(ReportAccessLevel.Comment);
    }

    [Test]
    public async Task ShareWithUser_UserNotFound_ThrowsNotFound()
    {
        _userRepository.Setup(e => e.FindUsersByLoginOrEmailAsync("nobody", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var act = async () => await _service.ShareWithUserAsync(ReportId, OwnerId, "nobody",
            ReportAccessLevel.Read);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShareWithUser_AmbiguousUser_ThrowsBadRequest()
    {
        _userRepository.Setup(e => e.FindUsersByLoginOrEmailAsync("ivan", It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateUser("ivan", "ivan@example.com"),
                new UserInfo { Id = Guid.NewGuid(), Accounts = [] }]);

        var act = async () => await _service.ShareWithUserAsync(ReportId, OwnerId, "ivan",
            ReportAccessLevel.Read);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ShareWithUser_Owner_ThrowsBadRequest()
    {
        _userRepository.Setup(e => e.FindUsersByLoginOrEmailAsync("owner", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserInfo { Id = OwnerId, Accounts = [] }]);

        var act = async () => await _service.ShareWithUserAsync(ReportId, OwnerId, "owner",
            ReportAccessLevel.Read);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task ShareWithUser_AlreadyShared_ThrowsBadRequest()
    {
        _userRepository.Setup(e => e.FindUsersByLoginOrEmailAsync("ivan", It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateUser("ivan", "ivan@example.com")]);
        _shareRepository.Setup(e => e.GetShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShare(ReportAccessLevel.Read));

        var act = async () => await _service.ShareWithUserAsync(ReportId, OwnerId, "ivan",
            ReportAccessLevel.Read);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [TestCase(ReportAccessLevel.Owner)]
    [TestCase((ReportAccessLevel)99)]
    public async Task ShareWithUser_InvalidLevel_ThrowsBadRequest(ReportAccessLevel level)
    {
        var act = async () => await _service.ShareWithUserAsync(ReportId, OwnerId, "ivan", level);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepository.Verify(
            e => e.FindUsersByLoginOrEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateShare_ChangesLevel()
    {
        _shareRepository.SetupSequence(e => e.GetShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShare(ReportAccessLevel.Read))
            .ReturnsAsync(CreateShare(ReportAccessLevel.Edit));
        _shareRepository
            .Setup(e => e.UpdateShareAsync(ReportId, UserId, ReportAccessLevel.Edit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepository.Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser("ivan", "ivan@example.com"));

        var result = await _service.UpdateShareAsync(ReportId, OwnerId, UserId, ReportAccessLevel.Edit);

        result.Share.AccessLevel.Should().Be(ReportAccessLevel.Edit);
        result.User.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateShare_MissingShare_ThrowsNotFound()
    {
        _shareRepository.Setup(e => e.GetShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportShare?)null);

        var act = async () => await _service.UpdateShareAsync(ReportId, OwnerId, UserId,
            ReportAccessLevel.Edit);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RevokeShare_DeletesShare()
    {
        _shareRepository.Setup(e => e.DeleteShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.RevokeShareAsync(ReportId, OwnerId, UserId);

        _shareRepository.Verify(e => e.DeleteShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RevokeShare_MissingShare_ThrowsNotFound()
    {
        _shareRepository.Setup(e => e.DeleteShareAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _service.RevokeShareAsync(ReportId, OwnerId, UserId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task GetShares_EnrichesUsersAndToleratesAuthFailure()
    {
        var secondUserId = Guid.NewGuid();
        _shareRepository.Setup(e => e.GetSharesAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateShare(ReportAccessLevel.Read),
                new ReportShare
                {
                    ReportId = ReportId,
                    UserId = secondUserId,
                    AccessLevel = ReportAccessLevel.Edit,
                    CreatedAt = DateTime.UtcNow,
                },
            ]);
        _userRepository.Setup(e => e.GetUserByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser("ivan", "ivan@example.com"));
        _userRepository.Setup(e => e.GetUserByIdAsync(secondUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("auth is down"));

        var result = await _service.GetSharesAsync(ReportId, OwnerId);

        result.Should().HaveCount(2);
        result.Single(e => e.Share.UserId == UserId).User.Should().NotBeNull();
        result.Single(e => e.Share.UserId == secondUserId).User.Should().BeNull();
    }

    [Test]
    public async Task Link_CreateRotateRevoke_UsesRepository()
    {
        _shareRepository.Setup(e => e.CreateOrGetLinkAsync(ReportId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportShareLink
            {
                ReportId = ReportId,
                Token = "created",
                CreatedAt = DateTime.UtcNow,
            });
        _shareRepository.Setup(e => e.ReplaceLinkAsync(ReportId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportShareLink
            {
                ReportId = ReportId,
                Token = "rotated",
                CreatedAt = DateTime.UtcNow,
            });
        _shareRepository.Setup(e => e.DeleteLinkAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        (await _service.CreateLinkAsync(ReportId, OwnerId)).Token.Should().Be("created");
        (await _service.RotateLinkAsync(ReportId, OwnerId)).Token.Should().Be("rotated");
        await _service.RevokeLinkAsync(ReportId, OwnerId);
    }

    [Test]
    public async Task RevokeLink_Missing_ThrowsNotFound()
    {
        _shareRepository.Setup(e => e.DeleteLinkAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _service.RevokeLinkAsync(ReportId, OwnerId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task NonOwner_CannotManageShares()
    {
        _accessService
            .Setup(e => e.RequireOwnerAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException());

        var act = async () => await _service.GetSharesAsync(ReportId, UserId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Report CreateReport() => new()
    {
        Id = ReportId,
        OwnerId = OwnerId,
        SourceProvider = "File",
        Format = "Latex",
        CreatedAt = DateTime.UtcNow,
    };

    private static ReportShare CreateShare(ReportAccessLevel level) => new()
    {
        ReportId = ReportId,
        UserId = UserId,
        AccessLevel = level,
        CreatedAt = DateTime.UtcNow,
    };

    private static UserInfo CreateUser(string login, string email) => new()
    {
        Id = UserId,
        Accounts =
        [
            new AccountInfo
            {
                Provider = "GitHub",
                Id = "1",
                Login = login,
                Email = email,
                Name = "Ivan",
            },
        ],
    };
}
