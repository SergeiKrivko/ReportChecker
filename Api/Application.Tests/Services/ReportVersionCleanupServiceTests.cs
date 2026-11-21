using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace Application.Tests.Services;

[TestFixture]
public class ReportVersionCleanupServiceTests
{
    private static readonly Guid ReportId = Guid.NewGuid();

    private Mock<IReportRepository> _reportRepository = null!;
    private Mock<ICheckRepository> _checkRepository = null!;
    private Mock<IReportSourceRepository<FileReportSource>> _fileReportSourceRepository = null!;
    private Mock<IReportSourceRepository<LocalReportSource>> _localReportSourceRepository = null!;
    private Mock<ICheckSourceRepository<FileCheckSource>> _fileCheckSourceRepository = null!;
    private Mock<ICheckSourceRepository<LocalCheckSource>> _localCheckSourceRepository = null!;
    private Mock<ICheckSourceRepository<YandexDiskCheckSource>> _yandexDiskCheckSourceRepository = null!;
    private Mock<ICheckSourceRepository<GoogleDriveCheckSource>> _googleDriveCheckSourceRepository = null!;
    private Mock<IFileRepository> _fileRepository = null!;
    private ReportVersionCleanupService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _reportRepository = new Mock<IReportRepository>();
        _checkRepository = new Mock<ICheckRepository>();
        _fileReportSourceRepository = new Mock<IReportSourceRepository<FileReportSource>>();
        _localReportSourceRepository = new Mock<IReportSourceRepository<LocalReportSource>>();
        _fileCheckSourceRepository = new Mock<ICheckSourceRepository<FileCheckSource>>();
        _localCheckSourceRepository = new Mock<ICheckSourceRepository<LocalCheckSource>>();
        _yandexDiskCheckSourceRepository = new Mock<ICheckSourceRepository<YandexDiskCheckSource>>();
        _googleDriveCheckSourceRepository = new Mock<ICheckSourceRepository<GoogleDriveCheckSource>>();
        _fileRepository = new Mock<IFileRepository>();

        _fileReportSourceRepository
            .Setup(e => e.GetByReportIdAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportSource<FileReportSource>?)null);
        _localReportSourceRepository
            .Setup(e => e.GetByReportIdAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportSource<LocalReportSource>?)null);

        _service = new ReportVersionCleanupService(_reportRepository.Object, _checkRepository.Object,
            _fileReportSourceRepository.Object, _localReportSourceRepository.Object,
            _fileCheckSourceRepository.Object, _localCheckSourceRepository.Object,
            _yandexDiskCheckSourceRepository.Object,
            _googleDriveCheckSourceRepository.Object,
            _fileRepository.Object, NullLogger<ReportVersionCleanupService>.Instance);
    }

    [Test]
    public async Task Cleanup_ReportNotFound_DoesNothing()
    {
        _reportRepository.Setup(e => e.GetReportByIdAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Report?)null);

        await _service.CleanupOldVersionsAsync(ReportId);

        _checkRepository.Verify(e => e.GetAllChecksOfReportAsync(ReportId, It.IsAny<CancellationToken>()),
            Times.Never);
        _fileRepository.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Cleanup_GitHubSource_DoesNotTouchS3()
    {
        SetupReport("GitHub");
        SetupChecks(CreateCheck(ProgressStatus.Completed, 1), CreateCheck(ProgressStatus.Completed, 2));

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.VerifyNoOtherCalls();
        _fileCheckSourceRepository.Verify(
            e => e.GetByCheckIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _localCheckSourceRepository.Verify(
            e => e.GetByCheckIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_KeepsLatestAndLatestSuccessful_DeletesOtherVersions()
    {
        var completed = CreateCheck(ProgressStatus.Completed, 1);
        var failed = CreateCheck(ProgressStatus.Failed, 2);
        var inProgress = CreateCheck(ProgressStatus.InProgress, 3);
        SetupReport("File");
        SetupChecks(completed, failed, inProgress);
        var completedSource = AddFileSource(completed, "v1.tex");
        var failedSource = AddFileSource(failed, "v2.tex");
        var inProgressSource = AddFileSource(inProgress, "v3.tex");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, failedSource, "v2.tex"), Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, completedSource, It.IsAny<string>()), Times.Never);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, inProgressSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_AllVersionsSuccessful_KeepsOnlyLatest()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var second = CreateCheck(ProgressStatus.Completed, 2);
        var latest = CreateCheck(ProgressStatus.Completed, 3);
        SetupReport("File");
        SetupChecks(latest, first, second);
        var firstSource = AddFileSource(first, "v1.tex");
        var secondSource = AddFileSource(second, "v2.tex");
        var latestSource = AddFileSource(latest, "v3.tex");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, firstSource, "v1.tex"),
            Times.Once);
        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, secondSource, "v2.tex"),
            Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, latestSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_LatestFailed_KeepsLatestAndLastSuccessful()
    {
        var successful = CreateCheck(ProgressStatus.Completed, 1);
        var olderSuccessful = CreateCheck(ProgressStatus.Completed, 2);
        var failed = CreateCheck(ProgressStatus.Failed, 3);
        SetupReport("File");
        SetupChecks(successful, olderSuccessful, failed);
        var successfulSource = AddFileSource(successful, "v1.tex");
        var olderSuccessfulSource = AddFileSource(olderSuccessful, "v2.tex");
        var failedSource = AddFileSource(failed, "v3.tex");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, successfulSource, "v1.tex"),
            Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, olderSuccessfulSource, It.IsAny<string>()),
            Times.Never);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, failedSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_NoSuccessfulVersions_KeepsOnlyLatest()
    {
        var failed = CreateCheck(ProgressStatus.Failed, 1);
        var cancelled = CreateCheck(ProgressStatus.Cancelled, 2);
        SetupReport("File");
        SetupChecks(failed, cancelled);
        var failedSource = AddFileSource(failed, "v1.tex");
        var cancelledSource = AddFileSource(cancelled, "v2.tex");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, failedSource, "v1.tex"),
            Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, cancelledSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_LocalSource_DeletesFromLocalBucket()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var latest = CreateCheck(ProgressStatus.Completed, 2);
        SetupReport("Local");
        SetupChecks(first, latest);
        var firstSource = AddLocalSource(first, "v1.zip");
        var latestSource = AddLocalSource(latest, "v2.zip");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.LocalSources, firstSource, "v1.zip"),
            Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(It.IsAny<FileRepositoryBucket>(), latestSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_InitialReportSourceFile_IsKept()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var latest = CreateCheck(ProgressStatus.Completed, 2);
        SetupReport("File");
        SetupChecks(first, latest);
        var initialSourceId = AddFileSource(first, "initial.tex");
        var latestSource = AddFileSource(latest, "v2.tex");
        _fileReportSourceRepository
            .Setup(e => e.GetByReportIdAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportSource<FileReportSource>
            {
                Id = Guid.NewGuid(),
                ReportId = ReportId,
                Data = new FileReportSource { InitialFileId = initialSourceId },
            });

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, initialSourceId, It.IsAny<string>()), Times.Never);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, latestSource, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Cleanup_SourceWithoutFileName_DeletesByIdOnly()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var latest = CreateCheck(ProgressStatus.Completed, 2);
        SetupReport("File");
        SetupChecks(first, latest);
        var sourceWithoutName = AddFileSource(first);
        AddFileSource(latest, "v2.tex");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, sourceWithoutName),
            Times.Once);
        _fileRepository.Verify(
            e => e.DeleteFileAsync(It.IsAny<FileRepositoryBucket>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Cleanup_DeleteFails_ContinuesWithRemainingVersions()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var second = CreateCheck(ProgressStatus.Completed, 2);
        var latest = CreateCheck(ProgressStatus.Completed, 3);
        SetupReport("File");
        SetupChecks(first, second, latest);
        var firstSource = AddFileSource(first, "v1.tex");
        var secondSource = AddFileSource(second, "v2.tex");
        AddFileSource(latest, "v3.tex");

        _fileRepository
            .Setup(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, firstSource, "v1.tex"))
            .ThrowsAsync(new InvalidOperationException("S3 is unavailable"));

        var act = async () => await _service.CleanupOldVersionsAsync(ReportId);

        await act.Should().NotThrowAsync();
        _fileRepository.Verify(e => e.DeleteFileAsync(FileRepositoryBucket.Sources, secondSource, "v2.tex"),
            Times.Once);
    }

    [Test]
    public async Task Cleanup_CheckWithoutSource_IsSkipped()
    {
        var first = CreateCheck(ProgressStatus.Completed, 1);
        var latest = CreateCheck(ProgressStatus.Completed, 2);
        SetupReport("File");
        SetupChecks(first, latest);
        AddFileSource(latest, "v2.tex");

        var act = async () => await _service.CleanupOldVersionsAsync(ReportId);

        await act.Should().NotThrowAsync();
        _fileRepository.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Cleanup_GoogleDriveSource_DeletesOldSnapshotsFromSourcesBucket()
    {
        var completed = CreateCheck(ProgressStatus.Completed, 1);
        var latest = CreateCheck(ProgressStatus.Completed, 2);
        SetupReport("GoogleDrive");
        SetupChecks(completed, latest);
        var completedSource = AddGoogleDriveSource(completed, "sources.zip");
        AddGoogleDriveSource(latest, "sources.zip");

        await _service.CleanupOldVersionsAsync(ReportId);

        _fileRepository.Verify(
            e => e.DeleteFileAsync(FileRepositoryBucket.Sources, completedSource, "sources.zip"), Times.Once);
        _yandexDiskCheckSourceRepository.VerifyNoOtherCalls();
    }

    private void SetupReport(string sourceProvider)
    {
        _reportRepository
            .Setup(e => e.GetReportByIdAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Report
            {
                Id = ReportId,
                OwnerId = Guid.NewGuid(),
                SourceProvider = sourceProvider,
                Format = "Latex",
            });
    }

    private void SetupChecks(params Check[] checks)
    {
        _checkRepository
            .Setup(e => e.GetAllChecksOfReportAsync(ReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checks);
    }

    private Guid AddFileSource(Check check, string? fileName = null)
    {
        var sourceId = Guid.NewGuid();
        _fileCheckSourceRepository
            .Setup(e => e.GetByCheckIdAsync(check.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckSource<FileCheckSource>
            {
                Id = sourceId,
                CheckId = check.Id,
                Data = new FileCheckSource { FileName = fileName, CreatedAt = check.CreatedAt },
            });
        return sourceId;
    }

    private Guid AddLocalSource(Check check, string? fileName = null)
    {
        var sourceId = Guid.NewGuid();
        _localCheckSourceRepository
            .Setup(e => e.GetByCheckIdAsync(check.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckSource<LocalCheckSource>
            {
                Id = sourceId,
                CheckId = check.Id,
                Data = new LocalCheckSource { FileName = fileName, CreatedAt = check.CreatedAt },
            });
        return sourceId;
    }

    private Guid AddGoogleDriveSource(Check check, string? fileName = null)
    {
        var sourceId = Guid.NewGuid();
        _googleDriveCheckSourceRepository
            .Setup(e => e.GetByCheckIdAsync(check.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckSource<GoogleDriveCheckSource>
            {
                Id = sourceId,
                CheckId = check.Id,
                Data = new GoogleDriveCheckSource
                {
                    FileId = "file-id",
                    ContentHash = "hash",
                    FileName = fileName,
                    CreatedAt = check.CreatedAt,
                },
            });
        return sourceId;
    }

    private static Check CreateCheck(ProgressStatus status, int order)
    {
        return new Check
        {
            Id = Guid.NewGuid(),
            ReportId = ReportId,
            UserId = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(order),
            Status = status,
        };
    }
}
