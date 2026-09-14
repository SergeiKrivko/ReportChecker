namespace ReportChecker.Abstractions;

public interface ICommentReadRepository
{
    public Task AddAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken ct = default);

    public Task DeleteAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken ct = default);
}