import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import type { Check, ProgressStatus, Report } from '../api/types';
import { isTerminalCheckStatus } from '../api/types';
import { Settings } from '../env';
import { FormatProvider } from '../formats/formatProvider';
import { LinkService, WorkspaceLink } from './linkService';
import { ReportCheckerApi } from '../api/reportCheckerApi';
import { log } from '../log';

export type CheckPhase = 'idle' | 'uploading' | 'queued' | 'inProgress' | 'completed' | 'failed';

export interface UploadResult {
  reportId: string;
  firstCheck: boolean;
}

/**
 * Загрузка отчета и новых версий + опрос статуса проверки.
 * Логика повторяет Studio.ReportService и цикл версий из Cli.Program.
 */
export class CheckService implements vscode.Disposable {
  private latestCheck?: Check;
  private pollTimer?: NodeJS.Timeout;
  private readonly pollListeners: ((check: Check | undefined) => void)[] = [];
  private busy = false;

  constructor(
    private readonly api: ReportCheckerApi,
    private readonly linkService: LinkService,
    private readonly settingsProvider: () => Settings,
    private readonly onPhase: (phase: CheckPhase, check?: Check) => void,
  ) {
  }

  getLatestCheck(): Check | undefined {
    return this.latestCheck;
  }

  onPoll(listener: (check: Check | undefined) => void): vscode.Disposable {
    this.pollListeners.push(listener);
    return new vscode.Disposable(() => {
      const index = this.pollListeners.indexOf(listener);
      if (index >= 0) this.pollListeners.splice(index, 1);
    });
  }

  /** Выбор главного файла: настройка → сохраненная связь → QuickPick. */
  async resolveEntryFile(provider: FormatProvider, forcePick = false): Promise<string | undefined> {
    const settings = this.settingsProvider();
    const link = await this.linkService.getLink();

    if (!forcePick) {
      if (settings.entryFile) {
        const abs = await this.toWorkspacePath(settings.entryFile);
        if (abs) return abs;
      }
      if (link?.entryFile) {
        const abs = await this.toWorkspacePath(link.entryFile);
        if (abs) return abs;
      }
    }

    const picked = await this.pickEntryFile(provider);
    return picked;
  }

  /** Создание нового отчета + первая проверка (как Studio.ReportService.CreateAsync). */
  async createReport(provider: FormatProvider, entryFile: string): Promise<WorkspaceLink> {
    this.setPhase('uploading');
    try {
      const pack = await provider.packSources(entryFile);
      const file = await this.api.uploadFile('Local', pack.fileName, pack.data);
      const link = await this.linkService.getLink();
      const clientId = await this.linkService.getClientId();
      const name = path.basename(entryFile);
      const reportId = await this.api.createReport({
        name,
        format: pack.format,
        sourceProvider: 'Local',
        source: {
          local: {
            initialFileId: file.id,
            entryFilePath: pack.entryFilePath,
            clientId,
            clientMachineName: os.hostname(),
          },
        },
        imageProcessingMode: 'Disable',
      });
      const newLink: WorkspaceLink = {
        reportId,
        entryFile: this.toRelative(entryFile),
        format: pack.format,
        sourceProvider: 'Local',
      };
      await this.linkService.setLink(newLink);
      void link;
      await this.startPolling(reportId);
      return newLink;
    } catch (e) {
      this.setPhase('failed');
      throw e;
    }
  }

  /** Отправка новой версии существующего отчета (как Cli UploadVersionAsync). */
  async uploadVersion(provider: FormatProvider, entryFile: string): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link) throw new Error('Проект не связан с отчетом');

    const latest = await this.safeLatestCheck(link.reportId);
    if (latest && !isTerminalCheckStatus(latest.status)) {
      throw new Error('Предыдущая проверка еще выполняется — новая версия не отправлена');
    }

    this.setPhase('uploading');
    try {
      const pack = await provider.packSources(entryFile);
      const file = await this.api.uploadFile('Local', pack.fileName, pack.data);
      await this.api.createCheck(link.reportId, {
        source: { id: file.id },
        name: `Version ${formatNow()}`,
      });
      await this.startPolling(link.reportId);
    } catch (e) {
      this.setPhase('failed');
      throw e;
    }
  }

  /** Загружает последнюю проверку без запуска опроса (при активации/обновлении). */
  async refreshLatestCheck(): Promise<Check | undefined> {
    const link = await this.linkService.getLink();
    if (!link) return undefined;
    const latest = await this.safeLatestCheck(link.reportId);
    if (latest) {
      this.latestCheck = latest;
      if (!isTerminalCheckStatus(latest.status)) {
        // Активный интервал уже опрашивает этот же отчет — не пересоздаем,
        // чтобы не плодить параллельные циклы (раньше это утекало таймеры)
        if (!this.pollTimer) {
          await this.startPolling(link.reportId);
        }
      } else {
        this.stopPolling();
        this.setPhase(latest.status === 'Completed' ? 'completed' : 'failed', latest);
      }
    }
    return latest;
  }

  async startPolling(reportId: string): Promise<void> {
    // Синхронная остановка: async-пауза создавала гонку — два параллельных
    // startPolling могли назначить два интервала, один из которых утекал навсегда
    // (чистился только «последний», и поллинг не заканчивался никогда).
    this.stopPolling();
    const intervalMs = Math.max(2, this.settingsProvider().pollIntervalSeconds) * 1000;
    const timer = setInterval(() => {
      void this.pollOnce(reportId);
    }, intervalMs);
    this.pollTimer = timer;
    void this.pollOnce(reportId);
  }

  stopPolling(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = undefined;
    }
  }

  dispose(): void {
    void this.stopPolling();
  }

  private async pollOnce(reportId: string): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    try {
      const check = await this.api.getLatestCheck(reportId);
      this.latestCheck = check;
      const status = check.status;
      log(`[check] status=${status ?? '<пусто>'} (${check.name ?? check.id})`);
      if (status === 'Queued') this.setPhase('queued', check);
      else if (status === 'InProgress' || status === 'CancellationRequested') this.setPhase('inProgress', check);
      else if (status === 'Completed') {
        this.setPhase('completed', check);
        this.stopPolling();
      } else if (status === 'Failed' || status === 'Cancelled') {
        this.setPhase('failed', check);
        this.stopPolling();
      } else {
        // Неизвестный статус: поллинг бессмыслен — останавливаемся и показываем ошибку
        log(`[check] неизвестный статус проверки: ${String(status)}`);
        this.setPhase('failed', check);
        this.stopPolling();
      }
      for (const l of this.pollListeners) l(check);
    } catch (e) {
      // сеть могла пропасть — продолжаем опрос, но с видимым следом в журнале
      log(`[check] ошибка опроса (продолжаем): ${e instanceof Error ? e.message : String(e)}`);
    } finally {
      this.busy = false;
    }
  }

  private setPhase(phase: CheckPhase, check?: Check): void {
    this.onPhase(phase, check);
  }

  private async safeLatestCheck(reportId: string): Promise<Check | undefined> {
    try {
      return await this.api.getLatestCheck(reportId);
    } catch {
      return undefined;
    }
  }

  private toRelative(absPath: string): string {
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder) return absPath;
    const rel = path.relative(folder.uri.fsPath, absPath);
    return rel.length > 0 ? rel : path.basename(absPath);
  }

  private async toWorkspacePath(rel: string): Promise<string | undefined> {
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder) return undefined;
    const abs = path.isAbsolute(rel) ? rel : path.join(folder.uri.fsPath, rel);
    try {
      const stat = await vscode.workspace.fs.stat(vscode.Uri.file(abs));
      return stat.type & vscode.FileType.File ? abs : undefined;
    } catch {
      return undefined;
    }
  }

  private async pickEntryFile(provider: FormatProvider): Promise<string | undefined> {
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (!folder) {
      void vscode.window.showErrorMessage('Откройте папку с отчетом');
      return undefined;
    }
    const matches = await vscode.workspace.findFiles(provider.watchGlob, '**/node_modules/**', 200);
    if (matches.length === 0) {
      void vscode.window.showErrorMessage(`В рабочей области не найдено файлов (${provider.watchGlob})`);
      return undefined;
    }

    // Читаем содержимое, чтобы предпочесть файл с \documentclass
    const contents = new Map<string, string>();
    for (const m of matches) {
      try {
        const doc = await vscode.workspace.openTextDocument(m);
        contents.set(m.fsPath, doc.getText().slice(0, 20000));
      } catch {
        contents.set(m.fsPath, '');
      }
    }

    const preferred = provider.pickBestEntry?.(matches.map((m) => m.fsPath), contents);
    interface EntryPickItem extends vscode.QuickPickItem {
      fsPath: string;
    }
    const items: EntryPickItem[] = matches
      .sort((a, b) => a.fsPath.localeCompare(b.fsPath))
      .map((m) => {
        const rel = path.relative(folder.uri.fsPath, m.fsPath).split(path.sep).join('/');
        const isBest = preferred && m.fsPath === preferred;
        return {
          label: isBest ? `$(file) ${rel}  $(star-full)` : `$(file) ${rel}`,
          detail: (contents.get(m.fsPath) ?? '').includes('\\documentclass')
            ? 'содержит \\documentclass'
            : undefined,
          fsPath: m.fsPath,
        };
      });

    const picked = await vscode.window.showQuickPick(items, {
      title: 'Выберите главный файл отчета',
      placeHolder: 'Файл, с которого начинается сборка отчета',
      matchOnDetail: true,
    });
    return picked?.fsPath;
  }

  async openInWeb(reportId: string): Promise<void> {
    const base = this.settingsProvider().webBaseUrl;
    await vscode.env.openExternal(vscode.Uri.parse(`${base}/reports/${reportId}`));
  }

  async getReport(): Promise<Report | undefined> {
    const link = await this.linkService.getLink();
    if (!link) return undefined;
    try {
      return await this.api.getReport(link.reportId);
    } catch {
      return undefined;
    }
  }
}

function formatNow(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

export type { ProgressStatus };
