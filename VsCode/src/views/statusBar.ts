import * as vscode from 'vscode';
import { CheckPhase } from '../services/checkService';

/** Status bar: состояние проверки и режим автоотправки. */
export class StatusBar implements vscode.Disposable {
  private readonly item: vscode.StatusBarItem;
  private phase: CheckPhase = 'idle';
  private unread = 0;
  private autoUpload = true;
  private loggedIn = false;
  private readonly disposables: vscode.Disposable[] = [];

  constructor() {
    this.item = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 50);
    this.disposables.push(this.item);
  }

  setPhase(phase: CheckPhase): void {
    this.phase = phase;
    this.render();
  }

  setUnread(count: number): void {
    this.unread = count;
    this.render();
  }

  setAutoUpload(enabled: boolean): void {
    this.autoUpload = enabled;
    this.render();
  }

  setLoggedIn(loggedIn: boolean): void {
    this.loggedIn = loggedIn;
    this.render();
  }

  private render(): void {
    if (!this.loggedIn) {
      this.item.hide();
      return;
    }
    this.item.show();
    const parts: string[] = [];
    let icon = '$(checklist)';
    switch (this.phase) {
      case 'uploading':
        icon = '$(cloud-upload)';
        parts.push('отправка…');
        break;
      case 'queued':
        icon = '$(watch)';
        parts.push('в очереди');
        break;
      case 'inProgress':
        icon = '$(sync~spin)';
        parts.push('проверка…');
        break;
      case 'failed':
        icon = '$(error)';
        parts.push('ошибка проверки');
        break;
      case 'completed':
        icon = '$(checklist)';
        break;
      default:
        break;
    }
    if (this.unread > 0) parts.push(`новых: ${this.unread}`);
    if (this.phase !== 'uploading' && this.phase !== 'queued' && this.phase !== 'inProgress') {
      parts.push(this.autoUpload ? 'авто ✓' : 'авто ✗');
    }
    this.item.text = `${icon} ReportChecker${parts.length ? `: ${parts.join(' · ')}` : ''}`;
    this.item.tooltip = 'ReportChecker';
    this.item.command = 'reportchecker.refreshIssues';
  }

  dispose(): void {
    for (const d of this.disposables) d.dispose();
  }
}
