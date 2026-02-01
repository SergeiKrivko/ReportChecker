namespace ReportChecker.Abstractions;

public interface ISourceProvider
{
    public string Key { get; }

    public Task<Stream> GetStreamAsync(string source);
    public Task<SourceSchema> FindSourceAsync(string source);
}

public record SourceSchema(string Source, Stream Stream, string? Name = null);