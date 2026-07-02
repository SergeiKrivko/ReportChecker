namespace Studio.LanguageProviders.Latex.Models;

public class LatexLibrary
{
    // public required string Name { get; init; }
    public LatexCommand[] Commands { get; init; } = [];
    public LatexEnvironment[] Environments { get; init; } = [];
}