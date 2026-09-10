import * as vscode from 'vscode';
import * as path from 'path';
import type { Issue } from '../api/types';
import { FormatProvider, FileIssue } from '../formats/formatProvider';
import { LinkService } from './linkService';
import { ReportCheckerApi } from '../api/reportCheckerApi';
import { log } from '../log';

/**
 * Загрузка ошибок отчета и привязка их к файлам рабочей области
 * (аналог Studio.IssueService, без кэша — файлы всегда под рукой).
 */
export class IssueService {
  private fileIssues: FileIssue[] = [];
  private readonly listeners: ((issues: FileIssue[]) => void)[] = [];
  private readonly unreadListeners: ((count: number) => void)[] = [];

  constructor(
    private readonly api: ReportCheckerApi,
    private readonly linkService: LinkService,
  ) {
  }

  getFileIssues(): FileIssue[] {
    return this.fileIssues;
  }

  onIssues(listener: (issues: FileIssue[]) => void): vscode.Disposable {
    this.listeners.push(listener);
    return new vscode.Disposable(() => {
      const index = this.listeners.indexOf(listener);
      if (index >= 0) this.listeners.splice(index, 1);
    });
  }

  onUnreadCount(listener: (count: number) => void): vscode.Disposable {
    this.unreadListeners.push(listener);
    return new vscode.Disposable(() => {
      const index = this.unreadListeners.indexOf(listener);
      if (index >= 0) this.unreadListeners.splice(index, 1);
    });
  }

  async reload(): Promise<FileIssue[]> {
    const link = await this.linkService.getLink();
    if (!link) {
      this.setIssues([]);
      return [];
    }
    const provider = LinkService.providerFor(link);
    if (!provider) {
      void vscode.window.showWarningMessage(
        `Формат отчета «${link.format}» не поддерживается плагином`,
      );
      this.setIssues([]);
      return [];
    }

    try {
      const issues = await this.api.getAllIssues(link.reportId);
      const entry = await this.resolveEntry(provider, link.entryFile);
      log(`[issues] entry=${entry ?? '<не найден>'}, ошибок: ${issues.length}`);
      if (!entry) {
        this.setIssues([]);
        return [];
      }
      const fileIssues = await provider.issuesToFileIssues(entry, issues);
      const mapped = fileIssues.filter((fi) => fi.position).length;
      const unmapped = fileIssues.filter((fi) => !fi.position);
      log(`[issues] привязано ${mapped}/${fileIssues.length}` +
        (unmapped.length > 0
          ? `; без позиции: ${unmapped.map((fi) => `«${fi.issue.chapter ?? '?'}» #${fi.issue.line ?? '?'}`).join(', ')}`
          : ''));
      // Диагностика маппинга: главы, которые парсер видит в текущих файлах
      if (provider.listChapters) {
        const chapters = await provider.listChapters(entry);
        log(`[chapters] найдено ${chapters.length}: ${chapters.map((c) => `«${c.chapter}» (${path.basename(c.file)}:${c.line})`).join(', ')}`);
      }
      this.setIssues(fileIssues);
      return fileIssues;
    } catch (e) {
      void vscode.window.showWarningMessage(
        `Не удалось загрузить список ошибок: ${e instanceof Error ? e.message : String(e)}`,
      );
      this.setIssues([]);
      return [];
    }
  }

  unreadCount(): number {
    return this.fileIssues.filter((fi) =>
      fi.issue.comments?.some((c) => c.isRead === false),
    ).length;
  }

  /** Отметить все непрочитанные комментарии ошибки прочитанными. */
  async markIssueRead(issue: Issue): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link) return;
    const unread = (issue.comments ?? []).filter((c) => c.isRead === false).map((c) => c.id!);
    if (unread.length === 0) return;
    try {
      await this.api.markRead(link.reportId, issue.id, { isRead: true, commentIds: unread });
      const local = this.fileIssues.find((fi) => fi.issue.id === issue.id);
      if (local) {
        for (const c of local.issue.comments ?? []) {
          if (c.isRead === false) c.isRead = true;
        }
        this.emit();
      }
    } catch (e) {
      void vscode.window.showWarningMessage(
        `Не удалось пометить комментарии прочитанными: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  private setIssues(issues: FileIssue[]): void {
    this.fileIssues = issues;
    this.emit();
  }

  private emit(): void {
    for (const l of this.listeners) l(this.fileIssues);
    const unread = this.unreadCount();
    for (const l of this.unreadListeners) l(unread);
  }

  private async resolveEntry(provider: FormatProvider, savedEntry: string): Promise<string | undefined> {
    const settings = vscode.workspace.getConfiguration('reportchecker');
    const configured = settings.get<string>('entryFile', '');
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder) return undefined;

    const candidates = [configured, savedEntry].filter((e) => e.length > 0);
    for (const rel of candidates) {
      const abs = path.isAbsolute(rel) ? rel : path.join(folder.uri.fsPath, rel);
      if (await fileExists(abs)) {
        log(`[entry] используется сохраненный файл: ${rel}`);
        return abs;
      }
    }
    // entry-файл мог переехать: повторно выбираем
    const matches = await vscode.workspace.findFiles(provider.watchGlob, '**/node_modules/**', 200);
    const contents = new Map<string, string>();
    for (const m of matches) {
      try {
        const doc = await vscode.workspace.openTextDocument(m);
        contents.set(m.fsPath, doc.getText().slice(0, 20000));
      } catch {
        contents.set(m.fsPath, '');
      }
    }
    const best = provider.pickBestEntry?.(matches.map((m) => m.fsPath), contents);
    log(`[entry] сохраненный файл не найден (${candidates.join(', ') || 'нет'}); авто-выбор: ${best ?? '<не найден>'} из ${matches.length} кандидатов`);
    return best;
  }
}

async function fileExists(p: string): Promise<boolean> {
  try {
    const stat = await vscode.workspace.fs.stat(vscode.Uri.file(p));
    return (stat.type & vscode.FileType.File) !== 0;
  } catch {
    return false;
  }
}
