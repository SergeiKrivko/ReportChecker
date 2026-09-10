import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { PatchLine } from '../api/types';
import { FormatProvider } from '../formats/formatProvider';
import { LinkService } from './linkService';

/**
 * Применение патча к локальному файлу через WorkspaceEdit (undo работает).
 */
export class PatchService {
  constructor(
    private readonly linkService: LinkService,
  ) {
  }

  async apply(chapter: string, patchLines: readonly PatchLine[]): Promise<boolean> {
    const link = await this.linkService.getLink();
    const provider = LinkService.providerFor(link);
    if (!link || !provider) throw new Error('Проект не связан с отчетом');

    const entry = await this.resolveEntry(provider, link.entryFile);
    if (!entry) throw new Error('Не найден главный файл отчета');

    const filePatch = await provider.patchToFilePatch(entry, chapter, patchLines);
    if (!filePatch) {
      void vscode.window.showWarningMessage(
        'Не удалось определить место исправления: глава не найдена в текущих файлах',
      );
      return false;
    }

    const uri = vscode.Uri.file(filePatch.path);
    const document = await vscode.workspace.openTextDocument(uri);
    const edit = new vscode.WorkspaceEdit();

    // Применяем от конца к началу, чтобы номера строк не поехали
    const lines = [...filePatch.lines].sort((a, b) => b.number - a.number);
    for (const line of lines) {
      const lineIndex = line.number - 1;
      if (lineIndex < 0 || lineIndex >= document.lineCount) {
        void vscode.window.showWarningMessage(
          `Строка ${line.number} вне диапазона файла ${filePatch.path}; файл изменился с момента проверки`,
        );
        return false;
      }
      const range = document.lineAt(lineIndex).range;

      if (line.type === 'Modify') {
        edit.replace(uri, range, line.content ?? '');
      } else if (line.type === 'Delete') {
        // удаляем строку вместе с переводом строки
        const end = lineIndex < document.lineCount - 1
          ? document.lineAt(lineIndex + 1).range.start
          : range.end;
        edit.delete(uri, new vscode.Range(range.start, end));
      } else if (line.type === 'Add') {
        const indent = detectIndent(document.lineAt(lineIndex));
        edit.insert(uri, range.end, `\n${indent}${line.content ?? ''}`);
      }
    }

    const ok = await vscode.workspace.applyEdit(edit);
    if (!ok) return false;

    const dirty = document.isDirty;
    if (!dirty) {
      await document.save();
    }
    return true;
  }

  /** Текущий текст строки (для подсветки диффа). */
  static lineContent(path: string, lineNumber: number): string | undefined {
    try {
      const text = fs.readFileSync(path, 'utf8');
      const lines = text.split(/\r?\n/);
      return lines[lineNumber - 1];
    } catch {
      return undefined;
    }
  }

  private async resolveEntry(provider: FormatProvider, savedEntry: string): Promise<string | undefined> {
    const configured = vscode.workspace.getConfiguration('reportchecker').get<string>('entryFile', '');
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder) return undefined;
    for (const rel of [configured, savedEntry].filter((e) => e.length > 0)) {
      const abs = path.isAbsolute(rel) ? rel : vscode.Uri.joinPath(folder.uri, ...rel.split(/[\\/]/)).fsPath;
      try {
        const stat = await vscode.workspace.fs.stat(vscode.Uri.file(abs));
        if (stat.type & vscode.FileType.File) return abs;
      } catch {
        /* нет файла */
      }
    }
    return undefined;
  }
}

function detectIndent(line: vscode.TextLine): string {
  const match = line.text.match(/^[ \t]*/);
  return match ? match[0] : '';
}
