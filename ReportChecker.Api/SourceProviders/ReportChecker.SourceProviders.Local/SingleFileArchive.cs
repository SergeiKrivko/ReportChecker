using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.SourceProviders.Local;

internal class SingleFileArchive(Stream stream, string? name) : IFileArchive
{
    public WriteMode WriteMode => WriteMode.External;

    public string? Name => name;

    public Task<Stream?> ReadAsync(string n)
    {
        return n == name ? ReadAsync() : Task.FromResult<Stream?>(null);
    }

    public Task<Stream?> ReadAsync()
    {
        return Task.FromResult<Stream?>(stream);
    }
}