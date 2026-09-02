using ReportChecker.Studio.Models;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex.Completions;

public class LatexLabelCompletion(string label) : ILanguageCompletion
{
    public string Name => label;
    public string Text => label;
    public string? Description => null;
}