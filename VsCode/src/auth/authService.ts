import * as vscode from 'vscode';
import { AuthApi } from '../api/authApi';
import type { UserCredentials } from '../api/types';
import { CLIENT_ID, CLIENT_SECRET, SECRETS_KEY, Settings } from '../env';
import { HttpClient } from '../api/httpClient';
import { LoopbackServer } from './loopbackServer';

export interface SessionInfo {
  userName?: string;
  email?: string;
}

/** Сессия: хранение refresh-токена в SecretStorage, авто-refresh, вход/выход. */
export class AuthService implements vscode.Disposable {
  private credentials?: UserCredentials;
  private refreshPromise?: Promise<void>;
  private readonly loopback = new LoopbackServer();
  private readonly disposables: vscode.Disposable[] = [];

  constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly authApi: AuthApi,
    private readonly settingsProvider: () => Settings,
    private readonly http: HttpClient,
    private readonly onSessionChanged: (loggedIn: boolean) => void,
  ) {
  }

  async initialize(): Promise<void> {
    const stored = await this.context.secrets.get(SECRETS_KEY);
    if (stored) {
      try {
        this.credentials = JSON.parse(stored) as UserCredentials;
      } catch {
        this.credentials = undefined;
      }
    }
    if (this.credentials?.refreshToken) {
      // Проверяем живучесть сессии в фоне, чтобы не блокировать активацию.
      void this.tryRefresh().catch(() => undefined);
    }
    this.onSessionChanged(this.isLoggedIn());
  }

  isLoggedIn(): boolean {
    return !!this.credentials?.accessToken || !!this.credentials?.refreshToken;
  }

  async getSessionInfo(): Promise<SessionInfo> {
    if (!this.isLoggedIn()) return {};
    const info: SessionInfo = { email: this.credentials?.email ?? undefined };
    try {
      const token = await this.getAccessToken();
      const user = await this.authApi.getUserInfo(token);
      info.userName = user.accounts?.[0]?.name ?? user.email ?? undefined;
      info.email = user.email ?? undefined;
    } catch {
      /* не критично */
    }
    return info;
  }

  /** Полный цикл входа: браузер → loopback → обмен кода. */
  async login(): Promise<boolean> {
    const settings = this.settingsProvider();
    const redirectUri = await this.loopback.resolveRedirectUri(settings.callbackPort);
    await this.loopback.start(settings.callbackPort);
    try {
      const url = this.authApi.authorizationUrl(settings.authProvider, redirectUri);
      const opened = await vscode.env.openExternal(vscode.Uri.parse(url));
      if (!opened) throw new Error('Не удалось открыть браузер');

      vscode.window.setStatusBarMessage('ReportChecker: ожидание авторизации в браузере…', 15_000);
      const code = await this.loopback.waitForCode();
      const credentials = await this.authApi.exchangeCode(code, redirectUri);
      await this.storeCredentials(credentials);
      this.onSessionChanged(true);
      return true;
    } finally {
      await this.loopback.stop();
    }
  }

  async logout(): Promise<void> {
    const refreshToken = this.credentials?.refreshToken;
    if (refreshToken) await this.authApi.revoke(refreshToken);
    this.credentials = undefined;
    await this.context.secrets.delete(SECRETS_KEY);
    this.onSessionChanged(false);
  }

  async getAccessToken(): Promise<string> {
    await this.ensureFreshToken();
    const token = this.credentials?.accessToken;
    if (!token) {
      throw new Error('Вы не авторизованы в ReportChecker');
    }
    return token;
  }

  async onUnauthorized(): Promise<void> {
    await this.tryRefresh();
    if (!this.credentials?.accessToken) {
      throw new Error('Сессия истекла. Войдите заново.');
    }
  }

  dispose(): void {
    for (const d of this.disposables) d.dispose();
    void this.loopback.stop();
  }

  private async ensureFreshToken(): Promise<void> {
    const credentials = this.credentials;
    if (!credentials) return;
    const expiresAt = credentials.expiresAt ?? 0;
    // Обновляем за минуту до истечения; при отсутствии данных — каждый раз доверяем серверу.
    if (expiresAt - Date.now() > 60_000) return;
    await this.tryRefresh();
  }

  private async tryRefresh(): Promise<void> {
    if (this.refreshPromise) return await this.refreshPromise;
    this.refreshPromise = (async () => {
      const refreshToken = this.credentials?.refreshToken;
      if (!refreshToken) throw new Error('Нет refresh-токена');
      try {
        const credentials = await this.authApi.refreshToken(refreshToken);
        await this.storeCredentials(credentials);
      } catch (e) {
        // refresh не удался — сбрасываем сессию
        this.credentials = undefined;
        await this.context.secrets.delete(SECRETS_KEY);
        this.onSessionChanged(false);
        throw new Error('Сессия истекла. Войдите заново.', { cause: e });
      } finally {
        this.refreshPromise = undefined;
      }
    })();
    return await this.refreshPromise;
  }

  private async storeCredentials(credentials: UserCredentials): Promise<void> {
    const expiresIn = credentials.expiresIn ?? 3600;
    this.credentials = {
      ...credentials,
      expiresAt: Date.now() + expiresIn * 1000,
    };
    await this.context.secrets.store(SECRETS_KEY, JSON.stringify(this.credentials));
  }
}

export function createAuthApi(settings: Settings, http: HttpClient): AuthApi {
  return new AuthApi(settings.authBaseUrl, CLIENT_ID, CLIENT_SECRET, http);
}
