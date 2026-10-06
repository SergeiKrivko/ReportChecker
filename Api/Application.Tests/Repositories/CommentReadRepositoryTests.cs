using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Entities;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.Models;

namespace Application.Tests.Repositories;

[TestFixture]
public class CommentReadRepositoryTests
{
    private SqliteConnection _connection = null!;
    private ReportCheckerDbContext _context = null!;
    private CommentReadRepository _repository = null!;
    private CommentRepository _commentRepository = null!;
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid IssueId = Guid.NewGuid();

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ReportCheckerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ReportCheckerDbContext(options);
        await _context.Database.MigrateAsync("20260707160303_IssueLocation");

        _repository = new CommentReadRepository(_context);
        _commentRepository = new CommentRepository(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [SetUp]
    public async Task SeedComments()
    {
        var reportId = Guid.NewGuid();
        var checkId = Guid.NewGuid();

        await _context.Reports.AddAsync(new ReportEntity
        {
            ReportId = reportId,
            OwnerId = UserId,
            SourceProvider = "file",
            Format = "docx",
            CreatedAt = DateTime.UtcNow,
        });
        await _context.Checks.AddAsync(new CheckEntity
        {
            CheckId = checkId,
            ReportId = reportId,
            UserId = UserId,
            CreatedAt = DateTime.UtcNow,
            Status = ProgressStatus.Completed,
        });
        await _context.Issues.AddAsync(new IssueEntity
        {
            IssueId = IssueId,
            CheckId = checkId,
            Title = "Test issue",
        });
        await _context.Comments.AddRangeAsync(
            new CommentEntity
            {
                CommentId = Guid.NewGuid(),
                IssueId = IssueId,
                UserId = UserId,
                Content = "First comment",
                CreatedAt = DateTime.UtcNow,
            },
            new CommentEntity
            {
                CommentId = Guid.NewGuid(),
                IssueId = IssueId,
                UserId = UserId,
                Content = "Second comment",
                CreatedAt = DateTime.UtcNow,
            });
        await _context.SaveChangesAsync();
    }

    private async Task<List<Guid>> GetCommentIdsAsync()
    {
        return await _context.Comments
            .Where(e => e.IssueId == IssueId)
            .Select(e => e.CommentId)
            .ToListAsync();
    }

    [Test]
    public async Task AddThenDelete_RoundTrip_ShouldMarkUnreadAgain()
    {
        var commentIds = await GetCommentIdsAsync();

        // Act: mark as read
        await _repository.AddAsync(UserId, commentIds);

        // Assert: all comments read for the user
        var comments = await _commentRepository.GetAllCommentsOfIssueAsync(IssueId, UserId);
        comments.Should().OnlyContain(e => e.IsRead == true);

        // Act: mark as unread (delete read markers)
        await _repository.DeleteAsync(UserId, commentIds);

        // Assert: all comments unread again
        _context.ChangeTracker.Clear();
        comments = await _commentRepository.GetAllCommentsOfIssueAsync(IssueId, UserId);
        comments.Should().OnlyContain(e => e.IsRead == false);
    }

    [Test]
    public async Task DeleteAsync_ShouldOnlyRemoveMarkersOfSpecifiedUser()
    {
        var otherUserId = Guid.NewGuid();
        var commentIds = await GetCommentIdsAsync();

        await _repository.AddAsync(UserId, commentIds);
        await _repository.AddAsync(otherUserId, commentIds);

        // Act: only first user's markers are removed
        await _repository.DeleteAsync(UserId, commentIds);

        // Assert: other user's read markers remain
        _context.ChangeTracker.Clear();
        var comments = await _commentRepository.GetAllCommentsOfIssueAsync(IssueId, otherUserId);
        comments.Should().OnlyContain(e => e.IsRead == true);
    }

    [Test]
    public async Task DeleteAsync_WithEmptyCollection_ShouldNotThrow()
    {
        // Act
        var act = async () => await _repository.DeleteAsync(UserId, []);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
