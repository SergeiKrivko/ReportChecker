using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace Studio.LanguageProviders.Latex;

public class LatexLanguageProviderFactory(IAlertService alertService) : ILanguageProviderFactory
{
    public string Key => "Latex";

    public async Task<ILanguageProvider> CreateProviderAsync(Project project, CancellationToken ct = default)
    {
        var provider = new LatexLanguageProvider(project.Path, alertService);
        await provider.InitializeAsync(ct);
        return provider;
    }
}