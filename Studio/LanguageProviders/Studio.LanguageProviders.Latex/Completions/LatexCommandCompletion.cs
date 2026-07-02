using ReportChecker.Studio.Models;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex.Completions;

public class LatexCommandCompletion(LatexCommand command) : ILanguageCompletion
{
    public string Name => "\\" + command.Name;
    public string Text => $"{command.Name}{{{string.Join(", ", command.Arguments
        .Where(a => !a.Optional)
        .Select((a, i) => a.Name ?? $"arg{i}"))}}}";
    public string? Description => command.Description + $"\n\n\\{Text}\n{string.Join('\n', command.Arguments
        .Select((a, i) => $"{a.Name ?? $"arg{i}"} - {a.Description}"))}";
    public int SelectFrom => command.Name.Length + 1;
    public int SelectLength => command.Arguments.Length == 0 ? 0 : (command.Arguments[0].Name ?? "arg0").Length;
}