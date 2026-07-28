using ReportChecker.Shared.Models;

namespace ReportChecker.Shared.Abstractions;

public interface IFormatProvider
{
    public string Key { get; }

    public Task<bool> TestSourceAsync(string path);

    public Task ApplyPatchAsync(string path, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default) => throw new NotSupportedException();

    public Task<SourcePack> PackSourcesAsync(string path);
    public Task<DateTime> GetUpdateTimeAsync(string path);

    public Task<FilePosition?> FilePositionByChapterPosition(string file, string chapter, int line,
        CancellationToken ct = default);

    public Task<IReadOnlyList<FileIssue>> IssuesToFileIssuesAsync(string path, IReadOnlyCollection<Issue> issues,
        CancellationToken ct = default);

    public Task<FilePatch?> PatchToFilePatchAsync(string path, string chapter, IEnumerable<PatchLine> patchLines,
        CancellationToken ct = default);
}