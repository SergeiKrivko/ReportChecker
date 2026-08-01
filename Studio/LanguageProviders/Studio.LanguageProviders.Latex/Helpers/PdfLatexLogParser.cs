using System.Text.RegularExpressions;
using ReportChecker.Studio.Models;

namespace Studio.LanguageProviders.Latex.Helpers;

public class PdfLatexLogParser
{
    public IReadOnlyList<BuildProblem> ParseLog(string logContent, string baseDirectory)
    {
        var problems = new List<BuildProblem>();

        string? currentError = null;
        foreach (var line in ParseCurrentFileMarkers(logContent.Replace("\r\n", "\n").Split('\n')))
        {
            if (line.Content.StartsWith('!'))
            {
                currentError = line.Content[2..];
            }
            else if (currentError != null)
            {
                int? lineNumber = null;
                if (line.Content.StartsWith("l."))
                    lineNumber = Convert.ToInt32(line.Content.Split(' ')[0][2..]);
                problems.Add(new BuildProblem
                {
                    FilePath = Path.IsPathRooted(line.File) ? line.File : Path.Join(baseDirectory, line.File),
                    LineNumber = lineNumber,
                    Message = currentError,
                    Type = BuildProblemType.Error
                });
                currentError = null;
            }
        }

        return problems;
    }

    private static IEnumerable<LogString> ParseCurrentFileMarkers(IEnumerable<string> lines)
    {
        var currentFile = "unknown.tex";
        foreach (var line in lines)
        {
            if (!line.StartsWith(" ("))
            {
                yield return new LogString(line, currentFile);
                continue;
            }

            var l = line.Trim().TrimStart('(');
            currentFile = l.Split(' ')[0];
        }
    }

    private record LogString(string Content, string File);
}