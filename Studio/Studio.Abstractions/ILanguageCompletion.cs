namespace ReportChecker.Studio.Abstractions;

public interface ILanguageCompletion
{
    public string Name { get; }
    public string Text { get; }
    public string? Description { get; }
    public int SelectFrom => 0;
    public int SelectLength => 0;
}