using FluentAssertions;
using Moq;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class ReportAccessServiceTests
{
    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private Mock<IReportRepository> _reportRepository = null!;
    private ReportAccessService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _reportRepository = new Mock<IReportRepository>();
        _service = new ReportAccessService(_reportRepository.Object);
    }

    [TestCase(ReportAccessLevel.Read, ReportAccessLevel.Read)]
    [TestCase(ReportAccessLevel.Comment, ReportAccessLevel.Read)]
    [TestCase(ReportAccessLevel.Edit, ReportAccessLevel.Comment)]
    [TestCase(ReportAccessLevel.Owner, ReportAccessLevel.Edit)]
    public async Task RequireAccess_SufficientLevel_ReturnsAccess(ReportAccessLevel actual,
        ReportAccessLevel required)
    {
        _reportRepository.Setup(e => e.GetReportAccessAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportAccess
            {
                Report = CreateReport(),
                Level = actual,
            });

        var result = await _service.RequireAccessAsync(ReportId, UserId, required);

        result.Level.Should().Be(actual);
    }

    [TestCase(ReportAccessLevel.Read, ReportAccessLevel.Comment)]
    [TestCase(ReportAccessLevel.Comment, ReportAccessLevel.Edit)]
    [TestCase(ReportAccessLevel.Edit, ReportAccessLevel.Owner)]
    public async Task RequireAccess_InsufficientLevel_ThrowsNotFound(ReportAccessLevel actual,
        ReportAccessLevel required)
    {
        _reportRepository.Setup(e => e.GetReportAccessAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportAccess
            {
                Report = CreateReport(),
                Level = actual,
            });

        var act = async () => await _service.RequireAccessAsync(ReportId, UserId, required);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RequireAccess_NoAccess_ThrowsNotFound()
    {
        _reportRepository.Setup(e => e.GetReportAccessAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportAccess?)null);

        var act = async () => await _service.RequireAccessAsync(ReportId, UserId, ReportAccessLevel.Read);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RequireOwner_Editor_ThrowsNotFound()
    {
        _reportRepository.Setup(e => e.GetReportAccessAsync(ReportId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportAccess
            {
                Report = CreateReport(),
                Level = ReportAccessLevel.Edit,
            });

        var act = async () => await _service.RequireOwnerAsync(ReportId, UserId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RequireSharedReport_ValidToken_ReturnsReport()
    {
        var report = CreateReport();
        _reportRepository.Setup(e => e.GetReportByShareTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _service.RequireSharedReportAsync("token");

        result.Should().Be(report);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("unknown")]
    public async Task RequireSharedReport_InvalidToken_ThrowsNotFound(string token)
    {
        _reportRepository.Setup(e => e.GetReportByShareTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report?)null);

        var act = async () => await _service.RequireSharedReportAsync(token);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Report CreateReport() => new()
    {
        Id = ReportId,
        OwnerId = Guid.NewGuid(),
        SourceProvider = "File",
        Format = "Latex",
        CreatedAt = DateTime.UtcNow,
    };
}
