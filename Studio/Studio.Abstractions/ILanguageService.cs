using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ILanguageService
{
    public IObservable<IReadOnlyList<BuildProblem>> BuildProblems { get; }

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset);
    public Task<BuildResult> BuildProjectAsync(CancellationToken ct = default);
}