import * as vscode from 'vscode';

/** Ошибка API с человеческим сообщением (ProblemDetails.title) и статус-кодом. */
export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

/** Ошибка сети (нет соединения, DNS и т.п.). */
export class NetworkError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'NetworkError';
  }
}

export class UnauthorizedError extends ApiError {
  constructor() {
    super(401, 'Требуется авторизация');
    this.name = 'UnauthorizedError';
  }
}

export function isNetworkError(e: unknown): boolean {
  return e instanceof NetworkError ||
    (e instanceof Error && (e.name === 'NetworkError' || e.name === 'TypeError' && /fetch/i.test(e.message)));
}

/**
 * Базовый HTTP-клиент: JSON, Bearer, понятные ошибки.
 * Все URL задаются полностью вызывающей стороной.
 */
export class HttpClient {
  constructor(
    private readonly getToken: () => Promise<string | undefined>,
    private readonly onUnauthorized: () => Promise<void>,
  ) {
  }

  private async headers(auth: boolean, extra?: Record<string, string>): Promise<Record<string, string>> {
    const headers: Record<string, string> = { ...(extra ?? {}) };
    if (auth) {
      const token = await this.getToken();
      if (!token) throw new UnauthorizedError();
      headers['Authorization'] = `Bearer ${token}`;
    }
    return headers;
  }

  private static async readError(response: Response): Promise<never> {
    let message = `Ошибка сервера (${response.status})`;
    let problem: unknown = null;
    try {
      problem = await response.json();
    } catch {
      try {
        const text = await response.text();
        if (text) message = text.slice(0, 300);
      } catch {
        /* тела нет */
      }
    }
    if (problem && typeof problem === 'object' && 'title' in (problem as Record<string, unknown>)) {
      const title = (problem as Record<string, unknown>)['title'];
      if (typeof title === 'string' && title.length > 0) message = title;
    }
    throw new ApiError(response.status, message);
  }

  async request(
    url: string,
    method: string,
    auth: boolean,
    extraHeaders?: Record<string, string>,
    body?: string | Uint8Array,
  ): Promise<Response> {
    let response: Response;
    try {
      response = await fetch(url, {
        method,
        headers: await this.headers(auth, extraHeaders),
        body,
      });
    } catch (e) {
      throw new NetworkError(e instanceof Error ? e.message : String(e));
    }

    if (response.status === 401 && auth) {
      await this.onUnauthorized();
      try {
        response = await fetch(url, {
          method,
          headers: await this.headers(auth, extraHeaders),
          body,
        });
      } catch (e) {
        throw new NetworkError(e instanceof Error ? e.message : String(e));
      }
    }

    if (!response.ok) await HttpClient.readError(response);
    return response;
  }

  async getJson<T>(url: string, auth = true): Promise<T> {
    const response = await this.request(url, 'GET', auth, { Accept: 'application/json' });
    return (await response.json()) as T;
  }

  async postJson<T>(url: string, schema: unknown, auth = true): Promise<T> {
    const response = await this.request(url, 'POST', auth, {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    }, JSON.stringify(schema));
    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  async postEmpty<T>(url: string, auth = true): Promise<T> {
    const response = await this.request(url, 'POST', auth, { Accept: 'application/json' });
    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  async putJson(url: string, schema: unknown, auth = true): Promise<void> {
    await this.request(url, 'PUT', auth, { 'Content-Type': 'application/json' }, JSON.stringify(schema));
  }

  async delete(url: string, auth = true): Promise<void> {
    await this.request(url, 'DELETE', auth);
  }

  async postForm<T>(url: string, form: FormData, auth = true): Promise<T> {
    // Content-Type выставит fetch вместе с boundary — не задаем его вручную.
    const headers: Record<string, string> = {};
    if (auth) {
      const token = await this.getToken();
      if (!token) throw new UnauthorizedError();
      headers['Authorization'] = `Bearer ${token}`;
    }
    let response: Response;
    try {
      response = await fetch(url, { method: 'POST', headers, body: form });
    } catch (e) {
      throw new NetworkError(e instanceof Error ? e.message : String(e));
    }
    if (response.status === 401 && auth) {
      await this.onUnauthorized();
      try {
        response = await fetch(url, { method: 'POST', headers: await this.headers(auth), body: form });
      } catch (e) {
        throw new NetworkError(e instanceof Error ? e.message : String(e));
      }
    }
    if (!response.ok) await HttpClient.readError(response);
    return (await response.json()) as T;
  }
}

export function toVsCodeError(e: unknown): vscode.MessageItem {
  if (e instanceof ApiError) {
    return { title: e.message };
  }
  return { title: e instanceof Error ? e.message : String(e) };
}
