import * as assert from 'assert';
import * as fflate from 'fflate';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { after, before, describe, it } from 'node:test';
import { LatexFormatProvider } from './latexProvider';
import { tryParseCommand } from './commandParser';
import type { Issue, PatchLine } from '../../api/types';

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'rc-latex-test-'));
}

function write(root: string, rel: string, content: string): string {
  const full = path.join(root, rel);
  fs.mkdirSync(path.dirname(full), { recursive: true });
  fs.writeFileSync(full, content, 'utf8');
  return full;
}

function issue(chapter: string | null, line: number | null, title = 'Т'): Issue {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    checkId: '00000000-0000-0000-0000-000000000002',
    title,
    status: 'Open',
    priority: 1,
    chapter,
    line,
    comments: [],
  };
}

describe('LatexCommandParser', () => {
  it('разбирает имя, опции и аргумент', () => {
    const cmd = tryParseCommand('\\section[short]{Длинный заголовок} % комментарий');
    assert.ok(cmd);
    assert.strictEqual(cmd.command, 'section');
    assert.strictEqual(cmd.options, 'short');
    assert.strictEqual(cmd.argument, 'Длинный заголовок');
  });

  it('не парсит обычные строки', () => {
    assert.strictEqual(tryParseCommand('Обычный текст'), null);
    assert.strictEqual(tryParseCommand('   \\documentclass{article}  ')!.command, 'documentclass');
  });
});

describe('LatexFormatProvider: главы и позиции', () => {
  let root = '';
  let entry = '';
  const provider = new LatexFormatProvider();

  before(() => {
    root = makeTempDir();
    entry = write(root, 'main.tex', [
      '\\documentclass{report}',
      '\\begin{document}',
      '\\include{intro}',
      '\\include{analysis}',
      '\\end{document}',
      '',
    ].join('\n'));
    write(root, 'intro.tex', [
      '\\chapter{Введение}',
      'строка 2',
      'строка 3',
      '',
    ].join('\n'));
    write(root, 'analysis.tex', [
      '\\chapter{Анализ}',
      'строка 2',
      '\\section{Подраздел}',
      'строка 4',
      'строка 5',
      '',
    ].join('\n'));
  });

  after(() => {
    fs.rmSync(root, { recursive: true, force: true });
  });

  it('маппит ошибку из главы в файл и строку', async () => {
    // Первый \chapter (level 0) сбрасывает путь: имя файла в ключе не остается
    const issues = [issue('Анализ//Подраздел', 2)];
    const fileIssues = await provider.issuesToFileIssues(entry, issues);
    assert.strictEqual(fileIssues.length, 1);
    assert.ok(fileIssues[0]!.position);
    assert.strictEqual(path.basename(fileIssues[0]!.position!.path), 'analysis.tex');
    assert.strictEqual(fileIssues[0]!.position!.line, 4);
  });

  it('filePositionByChapterPosition находит ту же строку', async () => {
    const pos = await provider.filePositionByChapterPosition(entry, 'Введение', 2);
    assert.ok(pos);
    assert.strictEqual(path.basename(pos!.path), 'intro.tex');
    assert.strictEqual(pos!.line, 2);
  });

  it('ошибки без позиции остаются без привязки', async () => {
    const fileIssues = await provider.issuesToFileIssues(entry, [issue('Нет такой глава', 1)]);
    assert.strictEqual(fileIssues.length, 1);
    assert.strictEqual(fileIssues[0]!.position, undefined);
  });
});

describe('LatexFormatProvider: патчи', () => {
  const provider = new LatexFormatProvider();
  const entries: Record<string, string> = {};

  before(() => {
    // Каждый тест — свой файл: патчи мутируют содержимое
    entries.modify = write(makeTempDir(), 'main.tex', [
      '\\documentclass{report}',
      '\\begin{document}',
      '\\chapter{Введение}',
      'первая',
      'вторая',
      'третья',
      '\\end{document}',
      '',
    ].join('\n'));
    entries.delete = write(makeTempDir(), 'main.tex', [
      '\\documentclass{report}',
      '\\chapter{Введение}',
      'первая',
      'вторая',
      '',
    ].join('\n'));
    entries.recalc = write(makeTempDir(), 'main.tex', [
      '\\documentclass{report}',
      '\\begin{document}',
      '\\chapter{Введение}',
      'текст',
      '',
    ].join('\n'));
  });

  after(() => {
    for (const file of Object.values(entries)) {
      fs.rmSync(path.dirname(file), { recursive: true, force: true });
    }
  });

  it('применяет Modify и Add', async () => {
    const entry = entries.modify!;
    // Строки главы нумеруются вместе с самим заголовком: \chapter — строка 1
    const lines: PatchLine[] = [
      { number: 3, content: 'ВТОРАЯ', type: 'Modify' },
      { number: 3, content: 'вставка', type: 'Add' },
    ];
    const applied = await provider.applyPatch(entry, 'Введение', lines);
    assert.strictEqual(applied, true);
    const text = fs.readFileSync(entry, 'utf8');
    assert.deepStrictEqual(text.split(/\r?\n/).slice(0, 9), [
      '\\documentclass{report}',
      '\\begin{document}',
      '\\chapter{Введение}',
      'первая',
      'ВТОРАЯ',
      'вставка',
      'третья',
      '\\end{document}',
      '',
    ]);
  });

  it('Delete выбрасывает строку', async () => {
    const entry = entries.delete!;
    // \chapter — строка 1 главы, «первая» — 2, «вторая» — 3
    const lines: PatchLine[] = [{ number: 3, type: 'Delete' }];
    const applied = await provider.applyPatch(entry, 'Введение', lines);
    assert.strictEqual(applied, true);
    const text = fs.readFileSync(entry, 'utf8');
    assert.ok(text.includes('первая'));
    assert.ok(!text.split(/\r?\n/).includes('вторая'));
  });

  it('patchToFilePatch пересчитывает координаты', async () => {
    const entry = entries.recalc!;
    const filePatch = await provider.patchToFilePatch(entry, 'Введение', [
      { number: 1, content: 'x', type: 'Modify' },
    ]);
    assert.ok(filePatch);
    assert.strictEqual(path.basename(filePatch!.path), 'main.tex');
    // строка 1 главы «Введение» = сам \chapter → строка 3 файла
    assert.strictEqual(filePatch!.lines[0]!.number, 3);
  });
});

describe('LatexFormatProvider: упаковка', () => {
  it('zip содержит все tex-файлы', async () => {
    const provider = new LatexFormatProvider();
    const root = makeTempDir();
    try {
      const entry = write(root, 'main.tex', 'x');
      write(root, 'sub/chapter1.tex', 'y');
      write(root, 'notes.txt', 'не должен попасть');
      const pack = await provider.packSources(entry);
      assert.strictEqual(pack.format, 'Latex');
      assert.strictEqual(pack.entryFilePath, 'main.tex');
      const unzipped = fflate.unzipSync(pack.data);
      const names = Object.keys(unzipped).sort();
      assert.deepStrictEqual(names, ['main.tex', 'sub/chapter1.tex']);
    } finally {
      fs.rmSync(root, { recursive: true, force: true });
    }
  });
});
