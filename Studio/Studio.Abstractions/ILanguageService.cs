using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ILanguageService
{
    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset);
}