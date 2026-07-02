namespace Studio.LanguageProviders.Latex.Models;

public class LatexEnvironment
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public LatexArgument[] Arguments { get; init; } = [];
}