using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ILanguageProviderFactory
{
    public string Key { get; }
    public Task<ILanguageProvider> CreateProviderAsync(Project project, CancellationToken ct = default);
}