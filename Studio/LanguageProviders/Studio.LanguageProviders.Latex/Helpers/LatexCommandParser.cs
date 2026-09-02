using System.Diagnostics.CodeAnalysis;

namespace Studio.LanguageProviders.Latex.Helpers;

internal static class LatexCommandParser
{
    extension(string line)
    {
        public bool TryParseCommand([NotNullWhen(true)] out ParsedLatexCommand? command)
        {
            line = line.Trim();
            command = null;
            if (!line.StartsWith('\\'))
                return false;
            var span = line.AsSpan();
            if (span.Contains('%'))
                span = span.Slice(0, span.IndexOf('%'));
            var name = span.Contains('[') ? span.Slice(1, span.IndexOf('[') - 1) :
                span.Contains('{') ? span.Slice(1, span.IndexOf('{') - 1) :
                span.Slice(1);

            string? options = null;
            string? argument = null;
            if (span.Contains('['))
            {
                var optionsSpan = span.Slice(span.IndexOf('['));
                if (optionsSpan.Contains(']'))
                    optionsSpan = optionsSpan.Slice(0, optionsSpan.IndexOf(']'));
                options = optionsSpan.TrimStart('[').TrimEnd(']').ToString();
            }
            if (span.Contains('{'))
            {
                var argumentSpan = span.Slice(span.IndexOf('{'));
                if (argumentSpan.Contains('}'))
                    argumentSpan = argumentSpan.Slice(0, argumentSpan.IndexOf('}'));
                argument = argumentSpan.TrimStart('{').TrimEnd('}').ToString();
            }

            command = new ParsedLatexCommand(name.ToString(), options, argument);
            return true;
        }

        public ParsedLatexCommand? ParseCommand()
        {
            line.TryParseCommand(out var command);
            return command;
        }
    }
}

internal record ParsedLatexCommand(string Command, string? Options, string? Argument);