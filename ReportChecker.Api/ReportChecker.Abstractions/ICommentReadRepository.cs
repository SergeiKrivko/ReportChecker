namespace ReportChecker.Abstractions;

public interface ICommentReadRepository
{
    public Task AddAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken ct = default);
}