using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ILanguageProvider
{
    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset);
    public Task<BuildResult> BuildAsync(CancellationToken ct = default);
}