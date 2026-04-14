using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Abstractions;

public interface IFormatProvider
{
    public string Key { get; }

    public Task<bool> TestSourceAsync(string path);

    public Task ApplyPatchAsync(string path, string chapter, IEnumerable<PatchLine> lines,
        CancellationToken ct = default) => throw new NotSupportedException();

    public Task<SourcePack> PackSourcesAsync(string path);
    public Task<DateTime> GetUpdateTimeAsync(string path);
}

public record SourcePack(Stream Stream, string FileName, string? EntryFilePath);