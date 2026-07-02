using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using Studio.LanguageProviders.Latex.Completions;
using Studio.LanguageProviders.Latex.Helpers;
using Studio.LanguageProviders.Latex.Models;

namespace Studio.LanguageProviders.Latex;

public class LatexLanguageProvider(string path) : ILanguageProvider
{
    private Dictionary<string, LatexLibrary> _libraries = [];

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var stream =
            Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Studio.LanguageProviders.Latex.Data.LatexCommands.json") ??
            throw new Exception("LaTeX data not found");
        _libraries =
            await JsonSerializer.DeserializeAsync<Dictionary<string, LatexLibrary>>(stream, JsonSerializerOptions,
                ct) ?? [];
    }

    private IEnumerable<LatexCommand> GetCommands()
    {
        return _libraries.SelectMany(e => e.Value.Commands);
    }

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset)
    {
        if (triggerText == "\\")
            return new LanguageCompletions(GetCommands()
                .Select(c => new LatexCommandCompletion(c)));
        var currentArgument = FindCurrentArgument(fileText, offset, out var match);
        if (currentArgument != null && match != null)
            return new LanguageCompletions(_libraries
                .SelectMany(l => l.Value.Environments)
                .Select(e => new LatexEnvironmentCompletion(e)), match.ArgumentStartOffset, match.ArgumentEndOffset);
        return LanguageCompletions.Empty();
    }

    private LatexArgument? FindCurrentArgument(string fileText, int offset, out LatexArgumentMatch? match)
    {
        var m = match = CurrentCommandParser.GetArgumentAtCursor(fileText, offset);
        if (match == null)
            return null;
        var command = GetCommands().FirstOrDefault(e => e.Name == m?.CommandName);
        if (command == null || match.ArgumentIndex >= command.Arguments.Length)
            return null;
        return command.Arguments[match.ArgumentIndex];
    }
}