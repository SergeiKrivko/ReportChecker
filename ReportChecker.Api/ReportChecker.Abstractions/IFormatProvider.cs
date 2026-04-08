using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface IFormatProvider
{
    public string Key { get; }

    public Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive);
    public Task<bool> TestSourceAsync(IFileArchive archive);

    public Task ApplyPatchAsync(IFileArchive archive, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default) => throw new NotSupportedException();
}