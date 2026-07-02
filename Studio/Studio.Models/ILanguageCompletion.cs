namespace ReportChecker.Studio.Models;

public interface ILanguageCompletion
{
    public string Name { get; }
    public string Text { get; }
    public string? Description { get; }
    public int SelectFrom => Text.Length;
    public int SelectLength => 0;
}