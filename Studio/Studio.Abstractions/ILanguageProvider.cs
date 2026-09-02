using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ILanguageProvider
{
    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset);
    public Task<BuildResult> BuildAsync(CancellationToken ct = default);
    public Task ParseAllAsync(CancellationToken ct = default);
    public Task ParseFileAsync(string path, CancellationToken ct = default);
    public Task ParseFileAsync(string path, string data, CancellationToken ct = default);
}