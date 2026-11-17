using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.Models;

namespace Application.Tests.Repositories;

[TestFixture]
public class ReportShareRepositoryTests
{
    private SqliteConnection _connection = null!;
    private ReportCheckerDbContext _context = null!;
    private ReportRepository _reportRepository = null!;
    private ReportShareRepository _shareRepository = null!;
    private Guid _reportId;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid SharedUserId = Guid.NewGuid();

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ReportCheckerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ReportCheckerDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _reportRepository = new ReportRepository(_context);
        _shareRepository = new ReportShareRepository(_context);
        _reportId = await _reportRepository.CreateReportAsync(OwnerId, "Report", "Latex", "File", null,
            ImageProcessingMode.Disable);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task GetReportAccess_Owner_ReturnsOwnerLevel()
    {
        var access = await _reportRepository.GetReportAccessAsync(_reportId, OwnerId);

        access.Should().NotBeNull();
        access!.Report.Id.Should().Be(_reportId);
        access.Level.Should().Be(ReportAccessLevel.Owner);
    }

    [TestCase(ReportAccessLevel.Read)]
    [TestCase(ReportAccessLevel.Comment)]
    [TestCase(ReportAccessLevel.Edit)]
    public async Task GetReportAccess_SharedUser_ReturnsAssignedLevel(ReportAccessLevel level)
    {
        await _shareRepository.CreateShareAsync(_reportId, SharedUserId, level);

        var access = await _reportRepository.GetReportAccessAsync(_reportId, SharedUserId);

        access.Should().NotBeNull();
        access!.Level.Should().Be(level);
    }

    [Test]
    public async Task GetReportAccess_Stranger_ReturnsNull()
    {
        var access = await _reportRepository.GetReportAccessAsync(_reportId, Guid.NewGuid());
        access.Should().BeNull();
    }

    [Test]
    public async Task GetReportAccess_DeletedReport_ReturnsNull()
    {
        await _reportRepository.DeleteReportAsync(_reportId);

        var access = await _reportRepository.GetReportAccessAsync(_reportId, OwnerId);
        access.Should().BeNull();
    }

    [Test]
    public async Task GetAllReportsOfUser_ReturnsOwnedAndShared()
    {
        var sharedReportId = await _reportRepository.CreateReportAsync(OwnerId, "Shared report", "Latex",
            "File", null, ImageProcessingMode.Disable);
        var ownReportId = await _reportRepository.CreateReportAsync(SharedUserId, "Own report", "Latex",
            "File", null, ImageProcessingMode.Disable);
        await _shareRepository.CreateShareAsync(sharedReportId, SharedUserId, ReportAccessLevel.Comment);

        var reports = (await _reportRepository.GetAllReportsOfUserAsync(SharedUserId)).ToList();

        reports.Should().HaveCount(2);
        reports.Single(e => e.Report.Id == ownReportId).Level.Should().Be(ReportAccessLevel.Owner);
        reports.Single(e => e.Report.Id == sharedReportId).Level.Should().Be(ReportAccessLevel.Comment);
    }

    [Test]
    public async Task GetAllReportsOfUser_ExcludesDeletedAndUnrelatedReports()
    {
        var deletedReportId = await _reportRepository.CreateReportAsync(SharedUserId, "Deleted", "Latex",
            "File", null, ImageProcessingMode.Disable);
        await _reportRepository.DeleteReportAsync(deletedReportId);
        await _reportRepository.CreateReportAsync(Guid.NewGuid(), "Unrelated", "Latex", "File", null,
            ImageProcessingMode.Disable);

        var reports = (await _reportRepository.GetAllReportsOfUserAsync(SharedUserId)).ToList();

        reports.Should().BeEmpty();
    }

    [Test]
    public async Task ShareCrud_RoundTrip()
    {
        var share = await _shareRepository.CreateShareAsync(_reportId, SharedUserId, ReportAccessLevel.Read);
        share.AccessLevel.Should().Be(ReportAccessLevel.Read);
        share.UpdatedAt.Should().BeNull();

        (await _shareRepository.UpdateShareAsync(_reportId, SharedUserId, ReportAccessLevel.Edit))
            .Should().BeTrue();
        var updated = await _shareRepository.GetShareAsync(_reportId, SharedUserId);
        updated!.AccessLevel.Should().Be(ReportAccessLevel.Edit);
        updated.UpdatedAt.Should().NotBeNull();

        (await _shareRepository.UpdateShareAsync(_reportId, Guid.NewGuid(), ReportAccessLevel.Edit))
            .Should().BeFalse();

        var shares = await _shareRepository.GetSharesAsync(_reportId);
        shares.Should().ContainSingle();

        (await _shareRepository.DeleteShareAsync(_reportId, SharedUserId)).Should().BeTrue();
        (await _shareRepository.DeleteShareAsync(_reportId, SharedUserId)).Should().BeFalse();
        (await _shareRepository.GetSharesAsync(_reportId)).Should().BeEmpty();
    }

    [Test]
    public async Task LinkCreateGetReplaceDelete_RoundTrip()
    {
        var link = await _shareRepository.CreateOrGetLinkAsync(_reportId, "first-token");
        link.Token.Should().Be("first-token");

        var existing = await _shareRepository.CreateOrGetLinkAsync(_reportId, "ignored-token");
        existing.Token.Should().Be("first-token");

        var replaced = await _shareRepository.ReplaceLinkAsync(_reportId, "second-token");
        replaced.Token.Should().Be("second-token");
        (await _shareRepository.GetLinkAsync(_reportId))!.Token.Should().Be("second-token");

        (await _shareRepository.DeleteLinkAsync(_reportId)).Should().BeTrue();
        (await _shareRepository.GetLinkAsync(_reportId)).Should().BeNull();
        (await _shareRepository.DeleteLinkAsync(_reportId)).Should().BeFalse();
    }

    [Test]
    public async Task GetReportByShareToken_ValidToken_ReturnsReport()
    {
        await _shareRepository.CreateOrGetLinkAsync(_reportId, "valid-token");

        var report = await _reportRepository.GetReportByShareTokenAsync("valid-token");

        report.Should().NotBeNull();
        report!.Id.Should().Be(_reportId);
        report.OwnerId.Should().Be(OwnerId);
    }

    [Test]
    public async Task GetReportByShareToken_InvalidToken_ReturnsNull()
    {
        var report = await _reportRepository.GetReportByShareTokenAsync("unknown-token");
        report.Should().BeNull();
    }

    [Test]
    public async Task GetReportByShareToken_DeletedReport_ReturnsNull()
    {
        await _shareRepository.CreateOrGetLinkAsync(_reportId, "deleted-token");
        await _reportRepository.DeleteReportAsync(_reportId);

        var report = await _reportRepository.GetReportByShareTokenAsync("deleted-token");
        report.Should().BeNull();
    }
}
