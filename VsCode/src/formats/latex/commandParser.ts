/**
 * Порт Shared.FormatProviders.Shared.FormatProviders.Latex.LatexCommandParser.
 * Разбирает команду LaTeX: \name[options]{argument}.
 */
export interface LatexCommand {
  command: string;
  options?: string;
  argument?: string;
}

export function tryParseCommand(rawLine: string): LatexCommand | null {
  const line = rawLine.trim();
  if (!line.startsWith('\\')) return null;

  let span = line;
  const percent = span.indexOf('%');
  if (percent >= 0) span = span.slice(0, percent);

  const bracket = span.indexOf('[');
  const brace = span.indexOf('{');
  const nameEnd = bracket >= 0 ? bracket : brace >= 0 ? brace : span.length;
  const name = span.slice(1, nameEnd);

  let options: string | undefined;
  if (bracket >= 0) {
    const close = span.indexOf(']', bracket);
    options = span.slice(bracket + 1, close >= 0 ? close : undefined);
  }
  let argument: string | undefined;
  if (brace >= 0) {
    const close = span.indexOf('}', brace);
    argument = span.slice(brace + 1, close >= 0 ? close : undefined);
  }

  return { command: name, options, argument };
}

/** Уровень структурной команды: chapter=0 … subsubsection=3; остальное — max. */
export function lineLevel(command: LatexCommand, outTitle: { title: string }): number {
  outTitle.title = command.argument ?? '';
  switch (command.command) {
    case 'chapter':
      return 0;
    case 'section':
      return 1;
    case 'subsection':
      return 2;
    case 'subsubsection':
      return 3;
    default:
      return Number.MAX_SAFE_INTEGER;
  }
}
