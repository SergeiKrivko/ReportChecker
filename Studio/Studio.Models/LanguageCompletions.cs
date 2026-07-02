namespace ReportChecker.Studio.Models;

public class LanguageCompletions
{
    public LanguageCompletions()
    {
    }

    public LanguageCompletions(IEnumerable<ILanguageCompletion> completions)
    {
        Completions = completions.ToList();
    }

    public LanguageCompletions(IEnumerable<ILanguageCompletion> completions, int startOffset)
    {
        Completions = completions.ToList();
        StartOffset = startOffset;
    }

    public LanguageCompletions(IEnumerable<ILanguageCompletion> completions, int startOffset, int endOffset)
    {
        Completions = completions.ToList();
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public static LanguageCompletions Empty() => new();

    public IReadOnlyList<ILanguageCompletion> Completions { get; init; } = [];
    public int Count => Completions.Count;
    public int StartOffset { get; init; } = -1;
    public int EndOffset { get; init; } = -1;
}