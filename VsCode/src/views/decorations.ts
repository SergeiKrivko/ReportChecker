import * as vscode from 'vscode';
import { FileIssue } from '../formats/formatProvider';

/**
 * Отметки ошибок в редакторе: иконка в гуттере, метка на overview ruler
 * и hover с кнопкой «Открыть комментарии».
 *
 * Строка не подсвечивается фоном: цвет берется из темы (theme color) для
 * overview ruler, а иконки гуттера — отдельные для светлой и темной тем.
 * Исправленные (Fixed) и закрытые (Closed) ошибки не отмечаются.
 * Отметки можно отключить настройкой reportchecker.editorDecorations.
 */

/** Метка ошибки — мягкий сине-голубой кружок, читаемый на обеих темах. */
const GUTTER_ICON_LIGHT = svgIcon('#2b7fd4');
const GUTTER_ICON_DARK = svgIcon('#5ca7e4');

function svgIcon(color: string): vscode.Uri {
  const svg =
    '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">' +
    `<circle cx="8" cy="8" r="4" fill="${color}"/></svg>`;
  return vscode.Uri.parse(`data:image/svg+xml;utf8,${encodeURIComponent(svg)}`);
}

export class IssueDecorations implements vscode.Disposable {
  private readonly decoration: vscode.TextEditorDecorationType;
  private current: FileIssue[] = [];
  private readonly disposables: vscode.Disposable[] = [];

  constructor() {
    this.decoration = vscode.window.createTextEditorDecorationType({
      isWholeLine: true,
      gutterIconSize: 'contain',
      overviewRulerLane: vscode.OverviewRulerLane.Right,
      // Цвет определяет тема — одинаково корректно на светлой и темной
      overviewRulerColor: new vscode.ThemeColor('editorOverviewRuler.infoForeground'),
      // Иконка гуттера — своя для светлой и темной тем
      light: { gutterIconPath: GUTTER_ICON_LIGHT },
      dark: { gutterIconPath: GUTTER_ICON_DARK },
    });
    this.disposables.push(this.decoration);
    this.disposables.push(
      vscode.window.onDidChangeActiveTextEditor(() => this.redraw()),
    );
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

  private redraw(): void {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    if (!this.enabled()) {
      editor.setDecorations(this.decoration, []);
      return;
    }
    const fileIssues = this.current.filter(
      (fi) =>
        fi.position &&
        fi.position.path.toLowerCase() === editor.document.uri.fsPath.toLowerCase() &&
        IssueDecorations.isVisible(fi),
    );
    editor.setDecorations(
      this.decoration,
      fileIssues.map((fi) => {
        const line = Math.min(Math.max(fi.position!.line - 1, 0), editor.document.lineCount - 1);
        const range = new vscode.Range(line, 0, line, editor.document.lineAt(line).text.length);
        return {
          range,
          hoverMessage: this.hover(fi),
        };
      }),
    );
  }

  /** Отмечаем только активные ошибки; исправленные и закрытые — нет. */
  private static isVisible(fi: FileIssue): boolean {
    return fi.issue.status === 'Open' || fi.issue.status === 'InProgress';
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
