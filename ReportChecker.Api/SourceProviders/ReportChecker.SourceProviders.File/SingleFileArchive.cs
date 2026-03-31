using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.File;

public class SingleFileArchive(Stream stream, string? name) : IFileArchive
{
    public string? Name => name;

    public Task<Stream?> OpenAsync(string n)
    {
        return n == name ? OpenAsync() : Task.FromResult<Stream?>(null);
    }

    public Task<Stream?> OpenAsync()
    {
        return Task.FromResult<Stream?>(stream);
    }
}