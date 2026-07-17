namespace ReportChecker.Studio.Models;

public class FileJump
{
    public string Path { get; }
    public int? Line { get; }
    public bool IsHandled { get; set; }

    public FileJump(string path)
    {
        Path = path;
    }

    public FileJump(string path, int line)
    {
        Path = path;
        Line = line;
    }
}