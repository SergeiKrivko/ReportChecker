namespace ReportChecker.Studio.Abstractions;

public interface ILanguageService
{
    public IReadOnlyList<ILanguageCompletion> GetCompletions(string triggerText);
}