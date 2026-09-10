import * as path from 'path';
import * as vscode from 'vscode';
import { isTerminalCheckStatus } from '../api/types';
import { Settings } from '../env';
import { FormatProvider } from '../formats/formatProvider';
import { CheckService } from './checkService';
import { LinkService } from './linkService';

/**
 * Автоотправка новой версии при изменении файлов отчета
 * (логика цикла версий из Cli.Program): watcher + debounce + guard по mtime.
 */
export class AutoUploadService implements vscode.Disposable {
  private watcher?: vscode.FileSystemWatcher;
  private timer?: NodeJS.Timeout;
  private pendingPaths = new Set<string>();
  private readonly disposables: vscode.Disposable[] = [];
  private paused = false;

  constructor(
    private readonly checkService: CheckService,
    private readonly linkService: LinkService,
    private readonly settingsProvider: () => Settings,
    private readonly getProvider: () => FormatProvider | undefined,
  ) {
  }

  /** Пересоздает watcher под формат текущей связи. */
  async attach(): Promise<void> {
    this.detach();
    const link = await this.linkService.getLink();
    const provider = LinkService.providerFor(link);
    if (!provider || !this.settingsProvider().autoUpload) return;

    this.watcher = vscode.workspace.createFileSystemWatcher(provider.watchGlob);
    this.disposables.push(this.watcher);
    this.disposables.push(
      this.watcher.onDidChange((uri) => this.schedule(uri.fsPath)),
      this.watcher.onDidCreate((uri) => this.schedule(uri.fsPath)),
      this.watcher.onDidDelete((uri) => this.schedule(uri.fsPath)),
    );
    this.disposables.push(
      vscode.workspace.onDidSaveTextDocument((doc) => {
        if (provider.isEntryCandidate(doc.uri.fsPath) || doc.uri.fsPath.toLowerCase().endsWith('.tex')) {
          this.schedule(doc.uri.fsPath);
        }
      }),
    );
  }

  detach(): void {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = undefined;
    }
    this.pendingPaths.clear();
    for (const d of this.disposables.splice(0)) d.dispose();
    this.watcher = undefined;
  }

  setPaused(paused: boolean): void {
    this.paused = paused;
  }

  isPaused(): boolean {
    return this.paused;
  }

  /** Немедленная проверка (используется после ручных действий). */
  async checkNow(): Promise<void> {
    await this.uploadIfNeeded();
  }

  private schedule(fsPath: string): void {
    if (!this.settingsProvider().autoUpload) return;
    this.pendingPaths.add(fsPath);
    if (this.timer) clearTimeout(this.timer);
    const delayMs = Math.max(5, this.settingsProvider().autoUploadDelaySeconds) * 1000;
    this.timer = setTimeout(() => {
      this.timer = undefined;
      void this.uploadIfNeeded();
    }, delayMs);
  }

  private async uploadIfNeeded(): Promise<void> {
    if (this.paused) return;
    const link = await this.linkService.getLink();
    const provider = LinkService.providerFor(link);
    if (!link || !provider) return;
    const entry = await this.resolveEntry(provider, link.entryFile);
    if (!entry) return;

    const latest = this.checkService.getLatestCheck();
    if (latest && !isTerminalCheckStatus(latest.status)) return; // проверка уже идет

    const lastCheckTime = latest?.createdAt ? Date.parse(latest.createdAt) : 0;
    const updateTime = await provider.getUpdateTime(entry);
    if (updateTime <= lastCheckTime) return; // изменений с последней проверки не было

    try {
      await this.checkService.uploadVersion(provider, entry);
      void vscode.window.setStatusBarMessage('ReportChecker: новая версия отправлена', 5000);
    } catch (e) {
      void vscode.window.showWarningMessage(
        `Не удалось автоотправить версию: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  private async resolveEntry(provider: FormatProvider, savedEntry: string): Promise<string | undefined> {
    const configured = this.settingsProvider().entryFile;
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

  dispose(): void {
    this.detach();
  }
}
