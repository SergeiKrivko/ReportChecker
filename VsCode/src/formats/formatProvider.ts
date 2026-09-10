import type { Issue, PatchLine } from '../api/types';

/** Позиция ошибки в файле рабочей области. */
export interface FilePosition {
  path: string;
  line: number;
}

/** Ошибка, привязанная к файлу (аналог Shared.Models.FileIssue). */
export interface FileIssue {
  issue: Issue;
  position?: FilePosition;
}

export interface SourcePack {
  format: string;
  fileName: string;
  entryFilePath: string;
  data: Uint8Array;
}

/**
 * Провайдер формата отчета — TS-аналог Shared.Abstractions.IFormatProvider.
 * Реестр по ключу формата; новые текстовые форматы добавляются новыми модулями.
 */
export interface FormatProvider {
  /** Ключ формата, совпадает с Report.format на сервере (например, "Latex"). */
  readonly key: string;

  /** Подходит ли файл как главный файл отчета. */
  isEntryCandidate(path: string): boolean;

  /** Глоб для поиска файлов отчета в рабочей области (для watcher'ов). */
  readonly watchGlob: string;

  /** Выбирает наиболее вероятный главный файл из кандидатов (может быть пусто). */
  pickBestEntry?(files: string[], contents: Map<string, string>): string | undefined;

  /** Упаковывает исходники отчета для отправки. */
  packSources(entryFilePath: string): Promise<SourcePack>;

  /** Максимальное время изменения файлов отчета (UTC, мс). */
  getUpdateTime(entryFilePath: string): Promise<number>;

  /** Привязывает ошибки (координаты главы/строки) к файлам рабочей области. */
  issuesToFileIssues(entryFilePath: string, issues: readonly Issue[]): Promise<FileIssue[]>;

  /** Глава/строка → файл/строка (для перехода к месту ошибки). */
  filePositionByChapterPosition(
    filePath: string,
    chapter: string,
    chapterLine: number,
  ): Promise<FilePosition | undefined>;

  /** Пересчитывает патч в координаты файла. */
  patchToFilePatch(
    filePath: string,
    chapter: string,
    patchLines: readonly PatchLine[],
  ): Promise<FilePatch | undefined>;

  /** Применяет патч к файлу (аналог ApplyPatchAsync). Возвращает true, если применился. */
  applyPatch(filePath: string, chapter: string, patchLines: readonly PatchLine[]): Promise<boolean>;

  /**
   * Список глав, которые парсер находит в текущих файлах (для диагностики
   * расхождений маппинга). Реализация может отсутствовать.
   */
  listChapters?(entryFilePath: string): Promise<{ chapter: string; file: string; line: number }[]>;
}

export interface FilePatch {
  path: string;
  lines: PatchLine[];
}

const providers = new Map<string, FormatProvider>();

export function registerFormatProvider(provider: FormatProvider): void {
  providers.set(provider.key, provider);
}

export function getFormatProvider(key: string): FormatProvider | undefined {
  return providers.get(key);
}

export function allFormatProviders(): FormatProvider[] {
  return [...providers.values()];
}
