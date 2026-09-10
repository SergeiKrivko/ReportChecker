import * as fflate from 'fflate';
import * as fs from 'fs';
import * as path from 'path';
import type { Issue, PatchLine } from '../../api/types';
import {
  FileIssue,
  FilePatch,
  FilePosition,
  FormatProvider,
  SourcePack,
} from '../formatProvider';
import { lineLevel, tryParseCommand } from './commandParser';

const CHAPTER_SEPARATOR = '//';

/**
 * Сервер кладет в архив патченный корневой файл под именем report.tex и при разборе
 * безымянного корня использует ключ главы "<root>"; чтобы маппинг совпадал,
 * корневой entry-файл с этим именем получаем тот же ключ.
 */
const ROOT_FILE_NAME = 'report.tex';
const ROOT_CHAPTER_KEY = '<root>';

/**
 * Порт Shared.FormatProviders.Latex.LatexFormatProvider.
 * Главы: \chapter/\section/\subsection/\subsubsection; обход \include; координаты
 * «глава//строка внутри главы» → файл/строка.
 */
export class LatexFormatProvider implements FormatProvider {
  readonly key = 'Latex';
  readonly watchGlob = '**/*.tex';

  isEntryCandidate(p: string): boolean {
    return p.toLowerCase().endsWith('.tex');
  }

  /**
   * Лучший кандидат в главные файлы: первый по числу \include, затем по наличию
   * \documentclass, затем по глубине пути. В проектах вида «report.tex подключает
   * preambula.tex, где объявлен \documentclass» главный файл — именно тот,
   * кто подключает остальные.
   */
  pickBestEntry(files: string[], contents: Map<string, string>): string | undefined {
    const score = (p: string): [number, number, number] => {
      const text = contents.get(p) ?? '';
      return [
        (text.match(/\\include\{[^}]*\}/g) ?? []).length,
        text.includes('\\documentclass') ? 1 : 0,
        -p.split(/[\\/]/).length,
      ];
    };
    return [...files].sort((a, b) => {
      const sa = score(a);
      const sb = score(b);
      for (let i = 0; i < 3; i++) {
        if (sa[i] !== sb[i]) return sb[i]! - sa[i]!;
      }
      return a.localeCompare(b);
    })[0];
  }

  async packSources(entryFilePath: string): Promise<SourcePack> {
    const rootPath = path.dirname(path.resolve(entryFilePath));
    const texFiles = listTexFiles(rootPath);
    const zipData = fflate.zipSync(
      Object.fromEntries(
        texFiles.map((file) => [
          toZipEntryName(rootPath, file),
          new Uint8Array(fs.readFileSync(file)),
        ]),
      ),
      { level: 6 },
    );
    return {
      format: this.key,
      fileName: path.basename(entryFilePath, path.extname(entryFilePath)) + '.zip',
      entryFilePath: path.basename(entryFilePath),
      data: zipData,
    };
  }

  async getUpdateTime(entryFilePath: string): Promise<number> {
    const root = path.dirname(path.resolve(entryFilePath));
    let max = 0;
    for (const file of listTexFiles(root)) {
      const mtime = fs.statSync(file).mtimeMs;
      if (mtime > max) max = mtime;
    }
    return max;
  }

  async issuesToFileIssues(entryFilePath: string, issues: readonly Issue[]): Promise<FileIssue[]> {
    const mapped = await this._issuesToFileIssues(entryFilePath, issues);
    // В отличие от Shared-версии, ошибки с ненайденной главой не выбрасываем,
    // а оставляем без привязки — они должны оставаться видимыми в панели.
    const mappedIds = new Set(mapped.map((fi) => fi.issue.id));
    const unmapped = issues
      .filter((i) => !mappedIds.has(i.id))
      .map((issue) => ({ issue }) as FileIssue);
    return [...mapped, ...unmapped];
  }

  private async _issuesToFileIssues(
    filePath: string,
    issues: readonly Issue[],
  ): Promise<FileIssue[]> {
    const fileName = path.basename(filePath);
    const directoryName = path.dirname(filePath);
    const lines = readLines(filePath);

    const result: FileIssue[] = [];
    const chapterPath: string[] = [
      fileName.toLowerCase() === ROOT_FILE_NAME ? ROOT_CHAPTER_KEY : fileName.replace(/^\//, ''),
    ];
    let currentChapter = chapterKey(chapterPath);
    let chapterIssues = issues
      .filter((e) => e.chapter === currentChapter)
      .sort((a, b) => (a.line ?? Number.MAX_SAFE_INTEGER) - (b.line ?? Number.MAX_SAFE_INTEGER));
    let chapterLineNumber = 0;
    let fileLineNumber = 0;
    const includedFiles: string[] = [];

    for (const line of lines) {
      fileLineNumber++;
      chapterLineNumber++;
      const command = tryParseCommand(line);
      if (command) {
        const title = { title: '' };
        const level = lineLevel(command, title);
        if (level <= 3) {
          result.push(...chapterIssues.map((e) => ({ issue: e })));
          while (level < chapterPath.length) chapterPath.pop();
          while (level > chapterPath.length) chapterPath.push('');
          chapterPath.push(title.title);
          currentChapter = chapterKey(chapterPath);
          chapterIssues = issues
            .filter((e) => e.chapter === currentChapter)
            .sort((a, b) => (a.line ?? Number.MAX_SAFE_INTEGER) - (b.line ?? Number.MAX_SAFE_INTEGER));
          chapterLineNumber = 1;
        } else if (command.command === 'include' && command.argument) {
          includedFiles.push(command.argument);
        }
      }

      while (chapterIssues.length > 0 && chapterIssues[0]!.line === chapterLineNumber) {
        result.push({
          issue: chapterIssues[0]!,
          position: { path: path.resolve(filePath), line: fileLineNumber },
        });
        chapterIssues.shift();
      }
    }

    const nested = await Promise.all(
      includedFiles.map((f) => this._issuesToFileIssues(resolveInclude(directoryName, f), issues)),
    );
    return nested.flat().concat(result);
  }

  async filePositionByChapterPosition(
    filePath: string,
    chapter: string,
    chapterLine: number,
  ): Promise<FilePosition | undefined> {
    return await this._filePositionByChapterPosition(filePath, chapter, chapterLine);
  }

  private async _filePositionByChapterPosition(
    filePath: string,
    chapter: string,
    issueChapterLine: number,
  ): Promise<FilePosition | undefined> {
    const fileName = path.basename(filePath);
    const directoryName = path.dirname(filePath);
    const lines = readLines(filePath);

    const chapterPath: string[] = [
      fileName.toLowerCase() === ROOT_FILE_NAME ? ROOT_CHAPTER_KEY : fileName.replace(/^\//, ''),
    ];
    let isPatchChapter = chapter === chapterKey(chapterPath);
    let lineNumber = 0;
    let fileLineNumber = 0;
    const includedFiles: string[] = [];

    for (const line of lines) {
      fileLineNumber++;
      const command = tryParseCommand(line);
      if (command) {
        const title = { title: '' };
        const level = lineLevel(command, title);
        if (level <= 3) {
          while (level < chapterPath.length) chapterPath.pop();
          while (level > chapterPath.length) chapterPath.push('');
          chapterPath.push(title.title);
          isPatchChapter = chapter === chapterKey(chapterPath);
        } else if (command.command === 'include' && command.argument) {
          includedFiles.push(command.argument);
        }
      }

      if (isPatchChapter) {
        lineNumber++;
        if (lineNumber === issueChapterLine) {
          return { path: path.resolve(filePath), line: fileLineNumber };
        }
      }
    }

    for (const f of includedFiles) {
      const res = await this._filePositionByChapterPosition(
        resolveInclude(directoryName, f),
        chapter,
        issueChapterLine,
      );
      if (res) return res;
    }
    return undefined;
  }

  async patchToFilePatch(
    filePath: string,
    chapter: string,
    patchLines: readonly PatchLine[],
  ): Promise<FilePatch | undefined> {
    return await this._patchToFilePatch(filePath, chapter, patchLines);
  }

  private async _patchToFilePatch(
    filePath: string,
    chapter: string,
    patchLines: readonly PatchLine[],
  ): Promise<FilePatch | undefined> {
    const fileName = path.basename(filePath);
    const directoryName = path.dirname(filePath);
    const lines = readLines(filePath);

    const chapterPath: string[] = [
      fileName.toLowerCase() === ROOT_FILE_NAME ? ROOT_CHAPTER_KEY : fileName.replace(/^\//, ''),
    ];
    let lineNumber = 0;
    const includedFiles: string[] = [];

    for (const line of lines) {
      const command = tryParseCommand(line);
      if (command) {
        const title = { title: '' };
        const level = lineLevel(command, title);
        if (level <= 3) {
          while (level < chapterPath.length) chapterPath.pop();
          while (level > chapterPath.length) chapterPath.push('');
          chapterPath.push(title.title);
          if (chapter === chapterKey(chapterPath)) {
            return {
              path: path.resolve(filePath),
              lines: patchLines.map((e) => ({
                number: e.number + lineNumber,
                content: e.content,
                previousContent: e.previousContent,
                type: e.type,
              })),
            };
          }
        } else if (command.command === 'include' && command.argument) {
          includedFiles.push(command.argument);
        }
      }
      lineNumber++;
    }

    for (const f of includedFiles) {
      const res = await this._patchToFilePatch(resolveInclude(directoryName, f), chapter, patchLines);
      if (res) return res;
    }
    return undefined;
  }

  async applyPatch(filePath: string, chapter: string, patchLines: readonly PatchLine[]): Promise<boolean> {
    return await this._applyPatch(filePath, chapter, patchLines);
  }

  /** Диагностика: какие главы парсер видит в файлах проекта (для сравнения с сервером). */
  async listChapters(entryFilePath: string): Promise<{ chapter: string; file: string; line: number }[]> {
    const visited = new Set<string>();
    const out: { chapter: string; file: string; line: number }[] = [];
    await this._listChapters(entryFilePath, visited, out);
    return out;
  }

  private async _listChapters(
    filePath: string,
    visited: Set<string>,
    out: { chapter: string; file: string; line: number }[],
  ): Promise<void> {
    const resolved = path.resolve(filePath);
    if (visited.has(resolved.toLowerCase())) return;
    visited.add(resolved.toLowerCase());
    const fileName = path.basename(filePath);
    const directoryName = path.dirname(filePath);

    const chapterPath: string[] = [
      fileName.toLowerCase() === ROOT_FILE_NAME ? ROOT_CHAPTER_KEY : fileName.replace(/^\//, ''),
    ];
    let currentChapter = chapterKey(chapterPath);
    // Начальная «глава» существует, даже если структурных команд в файле нет
    out.push({ chapter: currentChapter, file: resolved, line: 0 });
    let fileLineNumber = 0;
    const includedFiles: string[] = [];

    for (const line of readLines(filePath)) {
      fileLineNumber++;
      const command = tryParseCommand(line);
      if (command) {
        const title = { title: '' };
        const level = lineLevel(command, title);
        if (level <= 3) {
          while (level < chapterPath.length) chapterPath.pop();
          while (level > chapterPath.length) chapterPath.push('');
          chapterPath.push(title.title);
          const key = chapterKey(chapterPath);
          if (key !== currentChapter) {
            out.push({ chapter: key, file: resolved, line: fileLineNumber });
            currentChapter = key;
          }
        } else if (command.command === 'include' && command.argument) {
          includedFiles.push(command.argument);
        }
      }
    }

    for (const f of includedFiles) {
      await this._listChapters(resolveInclude(directoryName, f), visited, out);
    }
  }

  private async _applyPatch(
    filePath: string,
    chapter: string,
    patchLines: readonly PatchLine[],
  ): Promise<boolean> {
    const fileName = path.basename(filePath);
    const directoryName = path.dirname(filePath);
    const lines = patchLines.slice();
    const text = fs.readFileSync(filePath, 'utf8');
    const lst = text.split(/\r?\n/);

    const chapterPath: string[] = [
      fileName.toLowerCase() === ROOT_FILE_NAME ? ROOT_CHAPTER_KEY : fileName.replace(/^\//, ''),
    ];
    let isPatchChapter = chapter === chapterKey(chapterPath);
    let lineNumber = 0;
    let patchApplied = false;
    const includedFiles: string[] = [];
    const builder: string[] = [];

    for (const line of lst) {
      const command = tryParseCommand(line);
      if (command) {
        const title = { title: '' };
        const level = lineLevel(command, title);
        if (level <= 3) {
          while (level < chapterPath.length) chapterPath.pop();
          while (level > chapterPath.length) chapterPath.push('');
          chapterPath.push(title.title);
          isPatchChapter = chapter === chapterKey(chapterPath);
        } else if (command.command === 'include' && command.argument) {
          includedFiles.push(command.argument);
        }
      }

      if (isPatchChapter) {
        lineNumber++;
        const currentLines = lines.filter((e) => e.number === lineNumber);
        // как в C#: All() на пустом списке = true → исходная строка сохраняется
        if (currentLines.every((e) => e.type === 'Add')) {
          builder.push(line);
        } else if (currentLines.some((e) => e.type === 'Modify')) {
          const modifyLine = currentLines.find((e) => e.type === 'Modify')!;
          builder.push(modifyLine.content ?? '');
        }
        // только Delete (без Modify) → исходная строка выбрасывается

        for (const addLine of currentLines.filter((e) => e.type === 'Add')) {
          builder.push(addLine.content ?? '');
        }

        patchApplied = true;
      } else {
        builder.push(line);
      }
    }

    if (patchApplied) {
      const endsWithNewline = text.endsWith('\n');
      let newText = builder.join('\n');
      if (!endsWithNewline && newText.endsWith('\n')) newText = newText.slice(0, -1);
      fs.writeFileSync(filePath, newText, 'utf8');
    }

    let nestedApplied = false;
    for (const f of includedFiles) {
      nestedApplied =
        (await this._applyPatch(resolveInclude(directoryName, f), chapter, patchLines)) ||
        nestedApplied;
    }
    return patchApplied || nestedApplied;
  }
}

function chapterKey(chapterPath: readonly string[]): string {
  return chapterPath.filter((e) => e && e.trim().length > 0).join(CHAPTER_SEPARATOR);
}

function resolveInclude(directoryName: string, include: string): string {
  const withoutLeadingSlash = include.replace(/^\//, '');
  return path.resolve(directoryName, `${withoutLeadingSlash}.tex`);
}

function readLines(filePath: string): string[] {
  return fs.readFileSync(filePath, 'utf8').split(/\r?\n/);
}

function listTexFiles(root: string): string[] {
  const result: string[] = [];
  const stack = [root];
  while (stack.length > 0) {
    const dir = stack.pop()!;
    let entries: fs.Dirent[];
    try {
      entries = fs.readdirSync(dir, { withFileTypes: true });
    } catch {
      continue;
    }
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        stack.push(full);
      } else if (entry.isFile() && entry.name.toLowerCase().endsWith('.tex')) {
        result.push(full);
      }
    }
  }
  return result;
}

function toZipEntryName(root: string, file: string): string {
  const rel = path.relative(root, file).split(path.sep).join('/');
  return rel;
}
