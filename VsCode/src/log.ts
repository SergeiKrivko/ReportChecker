import * as vscode from 'vscode';

/**
 * Диагностический канал «ReportChecker» (Просмотр → Вывод).
 * Логируем выбор главного файла и разбор глав — помогает при расхождениях маппинга.
 */
let channel: vscode.OutputChannel | undefined;

function getChannel(): vscode.OutputChannel {
  if (!channel) channel = vscode.window.createOutputChannel('ReportChecker');
  return channel;
}

export function log(message: string): void {
  try {
    getChannel().appendLine(`[${new Date().toLocaleTimeString()}] ${message}`);
  } catch {
    /* без канала (тесты) — молча */
  }
}

export function showLog(): void {
  getChannel().show(true);
}
