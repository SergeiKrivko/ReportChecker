import { HttpClient, NetworkError } from './httpClient';
import type { UserAccount, UserCredentials } from './types';

/**
 * Клиент Avalux Auth (тот же протокол, что использует Avalux.Auth.UserClient):
 *  - GET  {auth}/api/v1/auth/authorize?provider={p}&client_id={id}&redirect_uri={uri}
 *  - POST {auth}/api/v1/auth/token  (form-urlencoded)
 *        grant_type=authorization_code | refresh_token
 *  - GET  {auth}/api/v1/auth/userinfo
 *  - POST {auth}/api/v1/auth/revoke
 */
export class AuthApi {
  constructor(
    private readonly baseUrl: string,
    private readonly clientId: string,
    private readonly clientSecret: string,
    private readonly http: HttpClient,
  ) {
  }

  authorizationUrl(provider: string, redirectUri: string): string {
    const query = new URLSearchParams({
      provider,
      client_id: this.clientId,
      redirect_uri: redirectUri,
    });
    return `${this.baseUrl}/api/v1/auth/authorize?${query.toString()}`;
  }

  async exchangeCode(code: string, redirectUri: string): Promise<UserCredentials> {
    const form = new URLSearchParams({
      grant_type: 'authorization_code',
      code,
      client_id: this.clientId,
      client_secret: this.clientSecret,
      redirect_uri: redirectUri,
    });
    return await this.token(form);
  }

  async refreshToken(refreshToken: string): Promise<UserCredentials> {
    const form = new URLSearchParams({
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
      client_id: this.clientId,
      client_secret: this.clientSecret,
    });
    return await this.token(form);
  }

  private async token(form: URLSearchParams): Promise<UserCredentials> {
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}/api/v1/auth/token`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: form.toString(),
      });
    } catch (e) {
      throw new NetworkError(e instanceof Error ? e.message : String(e));
    }
    if (!response.ok) {
      let detail = '';
      try {
        detail = (await response.text()).slice(0, 300);
      } catch { /* ignore */ }
      throw new NetworkError(`Сервер авторизации вернул ${response.status}${detail ? `: ${detail}` : ''}`);
    }

    // Толерантный парсинг: snake_case и camelCase.
    const raw = (await response.json()) as Record<string, unknown>;
    const accessToken = (raw['accessToken'] ?? raw['access_token']) as string | undefined;
    const refreshToken = (raw['refreshToken'] ?? raw['refresh_token']) as string | undefined;
    const expiresIn = (raw['expiresIn'] ?? raw['expires_in']) as number | undefined;
    const email = (raw['email'] ?? null) as string | null;
    if (!accessToken || !refreshToken) {
      throw new NetworkError('Некорректный ответ сервера авторизации: нет токенов');
    }
    return {
      accessToken,
      refreshToken,
      expiresIn: typeof expiresIn === 'number' ? expiresIn : undefined,
      email,
    };
  }

  async getUserInfo(accessToken: string): Promise<{ id: string; email?: string | null; accounts: UserAccount[] }> {
    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}/api/v1/auth/userinfo`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
    } catch (e) {
      throw new NetworkError(e instanceof Error ? e.message : String(e));
    }
    if (!response.ok) throw new NetworkError(`Не удалось получить данные пользователя (${response.status})`);
    const raw = (await response.json()) as Record<string, unknown>;
    const accounts = (raw['accounts'] as UserAccount[] | undefined) ?? [];
    return {
      id: String(raw['id'] ?? ''),
      email: (raw['email'] as string | null) ?? null,
      accounts,
    };
  }

  async revoke(refreshToken: string): Promise<void> {
    const form = new URLSearchParams({
      refresh_token: refreshToken,
      client_id: this.clientId,
      client_secret: this.clientSecret,
    });
    try {
      await fetch(`${this.baseUrl}/api/v1/auth/revoke`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: form.toString(),
      });
    } catch {
      // при выходе не критично
    }
  }
}

export { HttpClient };
