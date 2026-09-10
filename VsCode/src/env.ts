import * as vscode from 'vscode';

export interface Settings {
  apiBaseUrl: string;
  webBaseUrl: string;
  authBaseUrl: string;
  authProvider: string;
  callbackPort: number;
  autoUpload: boolean;
  autoUploadDelaySeconds: number;
  pollIntervalSeconds: number;
  entryFile: string;
}

export function readSettings(): Settings {
  const config = vscode.workspace.getConfiguration('reportchecker');
  return {
    apiBaseUrl: trimSlash(config.get<string>('apiBaseUrl', 'https://api.reportchecker.ru')),
    webBaseUrl: trimSlash(config.get<string>('webBaseUrl', 'https://report-checker.vercel.app')),
    authBaseUrl: trimSlash(config.get<string>('authBaseUrl', 'https://auth.nachert.art')),
    authProvider: config.get<string>('authProvider', 'password'),
    callbackPort: config.get<number>('callbackPort', 14872),
    autoUpload: config.get<boolean>('autoUpload', true),
    autoUploadDelaySeconds: config.get<number>('autoUploadDelaySeconds', 30),
    pollIntervalSeconds: config.get<number>('pollIntervalSeconds', 5),
    entryFile: config.get<string>('entryFile', ''),
  };
}

export function onDidChangeSettings(handler: () => void): vscode.Disposable {
  return vscode.workspace.onDidChangeConfiguration((e) => {
    if (e.affectsConfiguration('reportchecker')) handler();
  });
}

function trimSlash(url: string): string {
  return url.endsWith('/') ? url.slice(0, -1) : url;
}

/**
 * Идентификаторы клиента — те же, что зарегистрированы для десктоп-клиентов
 * (см. Shared/ApiClient/ServiceCollectionExtensions.cs).
 */
export const CLIENT_ID = '7c4a1272396979451d2a2d311f087050';
export const CLIENT_SECRET = '64305c89201a3fce41287675d2c9b0a5';

export const SECRETS_KEY = 'reportchecker.credentials';
