using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface IFormatProvider
{
    public string Key { get; }

    public Task<IEnumerable<Chapter>> GetChaptersAsync(IFileArchive archive);
    public Task<bool> TestSourceAsync(IFileArchive archive);

    public Task<CheckSourceUnion?> ApplyPatchAsync(IFileArchive archive, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default) => throw new NotSupportedException();
}