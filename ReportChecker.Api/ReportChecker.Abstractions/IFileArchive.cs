namespace ReportChecker.Abstractions;

public interface IFileArchive
{
    public string? Name { get; }
    public Task<Stream?> OpenAsync(string name);
    public Task<Stream?> OpenAsync();
}