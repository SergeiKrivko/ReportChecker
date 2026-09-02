namespace Studio.LanguageProviders.Latex.Helpers;

internal class FileParser(string rootPath)
{
    private readonly Dictionary<string, ParsedFile> _files = [];

    public IEnumerable<string> Labels => _files.Values
        .SelectMany(e => e.Labels);

    public IEnumerable<string> Bibliography => _files.Values
        .SelectMany(e => e.Bibliography);

    public async Task ParseAllAsync(CancellationToken ct = default)
    {
        List<string> stack = [rootPath];
        while (stack.Count > 0)
        {
            var p = stack[0];
            stack.RemoveAt(0);
            await ParseFileAsync(p, await File.ReadAllTextAsync(p, ct), ct);
            stack.AddRange(_files[p].Includes);
        }
    }

    public Task ParseFileAsync(string path, string data, CancellationToken ct = default)
    {
        Console.WriteLine($"Parse {path}");
        var lines = data.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var parsedFile = new ParsedFile
        {
            Path = path,
        };
        foreach (var line in lines)
        {
            if (line.TryParseCommand(out var command))
            {
                switch (command.Command)
                {
                    case "label":
                        parsedFile.Labels.Add(command.Argument ?? "");
                        break;
                    case "include":
                        parsedFile.Includes.Add(Path.Join(Path.GetDirectoryName(path),
                            Path.ChangeExtension(command.Argument ?? "", ".tex")));
                        break;
                    case "bibitem":
                        parsedFile.Bibliography.Add(command.Argument ?? "");
                        break;
                }
            }
        }

        _files[path] = parsedFile;
        return Task.CompletedTask;
    }

    public bool IsFileInProject(string path)
    {
        return _files.ContainsKey(path);
    }
}

internal class ParsedFile
{
    public required string Path { get; init; }
    public List<string> Labels { get; init; } = [];
    public List<string> Bibliography { get; init; } = [];
    public List<string> Includes { get; init; } = [];
}