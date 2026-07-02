namespace ReportChecker.Studio.Abstractions;

public interface ILanguageProvider
{
    public IReadOnlyList<ILanguageCompletion> GetCompletions(string triggerText);
}