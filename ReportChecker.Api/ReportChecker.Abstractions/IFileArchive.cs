using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface IFileArchive
{
    public string? Name { get; }
    public string? EntryFilePath => null;
    public Task<Stream?> ReadAsync(string name);
    public Task<Stream?> ReadAsync();
    public Task<CheckSourceUnion?> WriteAsync(string name, Stream content, CancellationToken ct) => throw new NotSupportedException();
    public Task<CheckSourceUnion?> WriteAsync(Stream content, CancellationToken ct) => throw new NotSupportedException();
}