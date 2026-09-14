import * as vscode from 'vscode';
import { FileIssue } from '../formats/formatProvider';

/**
 * Отметки ошибок в редакторе: иконка в гуттере по статусу ошибки (те же кодиконки
 * и цвета, что в дереве ошибок), метка на overview ruler и hover с кнопкой
 * «Открыть комментарии».
 *
 * Строка не подсвечивается фоном: цвет берется из темы для ruler, а иконки гуттера —
 * отдельные SVG (кодиконки) для светлой и темной тем. Если на одной строке несколько
 * ошибок, показывается самая приоритетная: сначала открытые (Open), затем
 * InProgress, затем закрытые (Closed) и исправленные (Fixed).
 * Отметки можно отключить настройкой reportchecker.editorDecorations.
 */

// Пути — оригинальные кодиконки VS Code (error, tools, circle-slash, check-all),
// как в дереве ошибок (issuesTree.issueIcon).
const CODICON_PATHS = {
  error:
    'M8 1C4.14 1 1 4.14 1 8C1 11.86 4.14 15 8 15C11.86 15 15 11.86 15 8C15 4.14 11.86 1 8 1ZM8 14C4.691 14 2 11.309 2 8C2 4.691 4.691 2 8 2C11.309 2 14 4.691 14 8C14 11.309 11.309 14 8 14ZM10.854 5.854L8.708 8L10.854 10.146C11.049 10.341 11.049 10.658 10.854 10.853C10.756 10.951 10.628 10.999 10.5 10.999C10.372 10.999 10.244 10.95 10.146 10.853L8 8.707L5.854 10.853C5.756 10.951 5.628 10.999 5.5 10.999C5.372 10.999 5.244 10.95 5.146 10.853C4.951 10.658 4.951 10.341 5.146 10.146L7.292 8L5.146 5.854C4.951 5.659 4.951 5.342 5.146 5.147C5.341 4.952 5.658 4.952 5.853 5.147L7.999 7.293L10.145 5.147C10.34 4.952 10.657 4.952 10.852 5.147C11.047 5.342 11.047 5.659 10.852 5.854H10.854Z',
  tools:
    'M5.66901 0.999997C5.52101 0.945997 5.34701 0.968997 5.21401 1.062C5.08101 1.155 5.00201 1.308 5.00201 1.47V3.286C5.00201 3.561 4.77701 3.786 4.50201 3.786C4.22701 3.786 4.00201 3.561 4.00201 3.286V1.47C4.00201 1.308 3.92301 1.156 3.79001 1.062C3.65801 0.967997 3.48501 0.945997 3.33501 0.999997C1.93901 1.495 1.00201 2.816 1.00201 4.287C1.00201 5.646 1.79201 6.876 3.00201 7.449V13.5C3.00201 14.327 3.67501 15 4.50201 15C5.32901 15 6.00201 14.327 6.00201 13.5V7.449C7.21201 6.876 8.00201 5.646 8.00201 4.287C8.00201 2.816 7.06401 1.495 5.66901 0.999997ZM5.33601 6.644C5.13601 6.714 5.00201 6.904 5.00201 7.116V13.501C5.00201 13.776 4.77701 14.001 4.50201 14.001C4.22701 14.001 4.00201 13.776 4.00201 13.501V7.116C4.00201 6.904 3.86801 6.715 3.66801 6.644C2.67201 6.292 2.00201 5.345 2.00201 4.288C2.00201 3.496 2.38501 2.765 3.00201 2.301V3.288C3.00201 4.115 3.67501 4.788 4.50201 4.788C5.32901 4.788 6.00201 4.115 6.00201 3.288V2.301C6.61901 2.765 7.00201 3.496 7.00201 4.288C7.00201 5.346 6.33201 6.293 5.33601 6.644ZM13.5 8H13.002V4.118L13.449 3.223C13.509 3.105 13.518 2.967 13.476 2.841L12.976 1.341C12.908 1.137 12.716 0.998997 12.501 0.998997H10.501C10.286 0.998997 10.095 1.137 10.026 1.341L9.52601 2.841C9.48401 2.967 9.49401 3.105 9.55301 3.223L10 4.118V8H9.50001C9.22401 8 9.00001 8.224 9.00001 8.5V12.5C9.00001 13.879 10.121 15 11.5 15C12.879 15 14 13.879 14 12.5V8.5C14 8.224 13.776 8 13.5 8ZM10.862 2.001H12.141L12.461 2.963L12.054 3.777C12.02 3.846 12.001 3.923 12.001 4.001V8.001H11.001V4.001C11.001 3.924 10.983 3.847 10.949 3.777L10.542 2.963L10.862 2.001ZM13.002 12.5C13.002 13.327 12.329 14 11.502 14C10.675 14 10.002 13.327 10.002 12.5V9H13.002V12.5Z',
  circleSlash:
    'M11.8746 3.41833C9.51718 1.42026 5.98144 1.53327 3.75736 3.75736C1.53327 5.98144 1.42026 9.51719 3.41833 11.8746L11.8746 3.41833ZM12.5817 4.12543L4.12543 12.5817C6.48282 14.5797 10.0186 14.4667 12.2426 12.2426C14.4667 10.0186 14.5797 6.48282 12.5817 4.12543ZM3.05025 3.05025C5.78392 0.316582 10.2161 0.316582 12.9497 3.05025C15.6834 5.78392 15.6834 10.2161 12.9497 12.9497C10.2161 15.6834 5.78392 15.6834 3.05025 12.9497C0.316583 10.2161 0.316582 5.78392 3.05025 3.05025Z',
  checkAll:
    'M12.354 3.646C12.159 3.451 11.842 3.451 11.647 3.646L6.70798 8.585L7.41498 9.292L12.354 4.353C12.549 4.158 12.549 3.841 12.354 3.646ZM1.85398 8.146C1.65898 7.951 1.34198 7.951 1.14698 8.146C0.951982 8.341 0.951982 8.658 1.14698 8.853L4.14698 11.853C4.24498 11.951 4.37298 11.999 4.50098 11.999C4.62898 11.999 4.75698 11.95 4.85498 11.853L5.20898 11.499L4.50198 10.792L1.85598 8.146H1.85398ZM7.49998 12C7.37198 12 7.24398 11.951 7.14598 11.854L4.14598 8.854C3.95098 8.659 3.95098 8.342 4.14598 8.147C4.34098 7.952 4.65798 7.952 4.85298 8.147L7.49898 10.793L14.645 3.647C14.84 3.452 15.157 3.452 15.352 3.647C15.547 3.842 15.547 4.159 15.352 4.354L7.85198 11.854C7.75398 11.952 7.62598 12 7.49798 12H7.49998Z',
} as const;

// Цвета — дефолтные цвета соответствующих ThemeColor из дерева:
// editorError.foreground, editorWarning.foreground, testing.iconQueued, testing.iconPassed.
const COLORS: Record<string, { light: string; dark: string }> = {
  error: { light: '#e51400', dark: '#f14c4c' },
  tools: { light: '#bf8803', dark: '#cca700' },
  circleSlash: { light: '#616161', dark: '#a5a5a5' },
  checkAll: { light: '#388a34', dark: '#89d185' },
};

function svgIcon(kind: keyof typeof CODICON_PATHS, theme: 'light' | 'dark'): vscode.Uri {
  const c = COLORS[kind];
  const color = (theme === 'light' ? c?.light : c?.dark) ?? '#888888';
  const svg =
    '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">' +
    `<path fill="${color}" d="${CODICON_PATHS[kind]}"/></svg>`;
  return vscode.Uri.parse(`data:image/svg+xml;utf8,${encodeURIComponent(svg)}`);
}

/** Есть ли у статуса оформленная отметка. */
function isKnownStatus(status: string | null | undefined): status is string {
  return typeof status === 'string' && STATUS_VIEWS[status] !== undefined;
}

/** Приоритет статуса на строке: меньше — важнее (открытые, затем закрытые). */
function statusPriority(status: string | null | undefined): number {
  switch (status) {
    case 'Open':
      return 0;
    case 'InProgress':
      return 1;
    case 'Closed':
      return 2;
    case 'Fixed':
      return 3;
    default:
      return 4;
  }
}

interface StatusView {
  kind: keyof typeof CODICON_PATHS;
  ruler: string;
}

const STATUS_VIEWS: Record<string, StatusView> = {
  Open: { kind: 'error', ruler: 'editorError.foreground' },
  InProgress: { kind: 'tools', ruler: 'editorWarning.foreground' },
  Closed: { kind: 'circleSlash', ruler: 'editorOverviewRuler.foreground' },
  Fixed: { kind: 'checkAll', ruler: 'testing.iconPassed' },
};

const FALLBACK_STATUS = 'Open';

export class IssueDecorations implements vscode.Disposable {
  private readonly decorations = new Map<string, vscode.TextEditorDecorationType>();
  private current: FileIssue[] = [];
  private readonly disposables: vscode.Disposable[] = [];

  constructor() {
    for (const [status, view] of Object.entries(STATUS_VIEWS)) {
      const t = vscode.window.createTextEditorDecorationType({
        isWholeLine: true,
        gutterIconSize: 'contain',
        overviewRulerLane: vscode.OverviewRulerLane.Right,
        // Цвет метки на ruler определяет тема — одинаково корректно на обеих темах
        overviewRulerColor: new vscode.ThemeColor(view.ruler),
        // Иконка гуттера — кодиконка статуса, своя для светлой и темной тем
        light: { gutterIconPath: svgIcon(view.kind, 'light') },
        dark: { gutterIconPath: svgIcon(view.kind, 'dark') },
      });
      this.decorations.set(status, t);
      this.disposables.push(t);
    }
    this.disposables.push(vscode.window.onDidChangeActiveTextEditor(() => this.redraw()));
    this.disposables.push(
      vscode.workspace.onDidChangeConfiguration((e) => {
        if (e.affectsConfiguration('reportchecker.editorDecorations')) this.redraw();
      }),
    );
  }

  update(issues: readonly FileIssue[]): void {
    this.current = [...issues];
    this.redraw();
  }

  private enabled(): boolean {
    return vscode.workspace
      .getConfiguration('reportchecker')
      .get<boolean>('editorDecorations', true);
  }

  private clear(editor: vscode.TextEditor): void {
    for (const t of this.decorations.values()) editor.setDecorations(t, []);
  }

  private redraw(): void {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    if (!this.enabled()) {
      this.clear(editor);
      return;
    }
    const fileIssues = this.current.filter(
      (fi) =>
        fi.position &&
        fi.position.path.toLowerCase() === editor.document.uri.fsPath.toLowerCase() &&
        isKnownStatus(fi.issue.status),
    );
    // На строке может быть несколько ошибок — рисуем самую приоритетную:
    // сначала открытые, затем inProgress, закрытые и исправленные.
    const byLine = new Map<number, FileIssue>();
    for (const fi of fileIssues) {
      const line = fi.position!.line;
      const prev = byLine.get(line);
      if (!prev || statusPriority(fi.issue.status) < statusPriority(prev.issue.status)) {
        byLine.set(line, fi);
      }
    }
    // Готовим наборы range'ов для каждого статуса и снимаем старые отметки
    const ranges = new Map<string, vscode.DecorationOptions[]>();
    for (const status of this.decorations.keys()) ranges.set(status, []);
    for (const [line, fi] of byLine) {
      const clamped = Math.min(Math.max(line - 1, 0), editor.document.lineCount - 1);
      const range = new vscode.Range(clamped, 0, clamped, editor.document.lineAt(clamped).text.length);
      const status = isKnownStatus(fi.issue.status) ? fi.issue.status : FALLBACK_STATUS;
      ranges.get(status)?.push({
        range,
        hoverMessage: this.hover(fi),
      });
    }
    for (const [status, t] of this.decorations) {
      editor.setDecorations(t, ranges.get(status) ?? []);
    }
  }

  private hover(fi: FileIssue): vscode.MarkdownString {
    const md = new vscode.MarkdownString();
    md.isTrusted = true;
    md.supportThemeIcons = true;
    md.appendMarkdown(`**${escapeMd(fi.issue.title ?? 'Ошибка')}**\n\n`);
    const comments = fi.issue.comments ?? [];
    md.appendMarkdown(`Комментариев: ${comments.length}\n\n`);
    md.appendMarkdown(
      `[$(comment-discussion) Открыть комментарии](command:reportchecker.openIssue?${encodeURIComponent(JSON.stringify([fi]))} "Открыть комментарии")`,
    );
    return md;
  }

  dispose(): void {
    for (const d of this.disposables) d.dispose();
  }
}

function escapeMd(s: string): string {
  return s.replace(/[*_`[\]]/g, '\\$&');
}
