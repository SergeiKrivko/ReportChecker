namespace Studio.LanguageProviders.Latex.Helpers;

internal class CurrentCommandParser
{
    private static LatexArgumentMatch? FindCommandArgument(string fileText, int startOffset)
    {
        var argumentIndex = 0;
        var argumentStart = startOffset;
        var offset = startOffset - 1;
        var level = 1;
        while (offset >= 0 && level > 0)
        {
            var symbol = fileText[offset];
            switch (symbol)
            {
                case ']':
                case '}':
                    level++;
                    break;
                case '[':
                case '{':
                    level--;
                    break;
                case ',':
                    if (level == 1)
                        argumentIndex++;
                    if (argumentIndex == 1)
                        argumentStart = offset + 1;
                    break;
                case '\r':
                case '\n':
                    return null;
            }

            offset--;
        }

        if (offset < 0)
            return null;
        if (argumentIndex == 0)
            argumentStart = offset + 2;
        var commandEnd = offset;
        while (offset >= 0 && fileText[offset] != '\\')
        {
            offset--;
        }

        if (offset < 0)
            return null;

        var commandStart = offset + 1;
        var commandName = fileText[commandStart..(commandEnd + 1)];

        offset = startOffset;
        while (offset < fileText.Length && !",}]\n\r;".Contains(fileText[offset]))
            offset++;

        return new LatexArgumentMatch
        {
            CommandName = commandName,
            CommandStartOffset = commandStart,
            CommandEndOffset = commandEnd,
            ArgumentIndex = argumentIndex,
            ArgumentStartOffset = argumentStart,
            ArgumentEndOffset = offset,
        };
    }

    public static LatexArgumentMatch? GetArgumentAtCursor(string fileText, int offset)
    {
        // Проверка границ
        if (string.IsNullOrEmpty(fileText) || offset < 0 || offset > fileText.Length)
            return null;
        return FindCommandArgument(fileText, offset);
    }
}

internal class LatexCommandMatch
{
    public required string CommandName { get; init; }
    public int CommandStartOffset { get; init; }
    public int CommandEndOffset { get; init; }
}

internal class LatexArgumentMatch : LatexCommandMatch
{
    public int ArgumentIndex { get; init; }
    public int ArgumentStartOffset { get; set; }
    public int ArgumentEndOffset { get; set; }
}