using ReportChecker.Studio.Models;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex.Completions;

public class LatexEnvironmentCompletion(LatexEnvironment environment) : ILanguageCompletion
{
    public string Name => environment.Name;
    public string Text => environment.Name;
    public string? Description => environment.Description;
}