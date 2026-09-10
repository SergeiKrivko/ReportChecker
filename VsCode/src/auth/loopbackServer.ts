import * as http from 'http';
import * as vscode from 'vscode';

/** Локальный HTTP-сервер, принимающий редирект с ?code=... после авторизации. */
export class LoopbackServer {
  private server?: http.Server;
  private port = 0;

  /** Запускает сервер и возвращает redirect_uri, который нужно передать на сервер авторизации. */
  async start(port: number): Promise<string> {
    if (this.server) throw new Error('Loopback-сервер уже запущен');
    this.port = port;
    const server = http.createServer((req, res) => this.handle(req, res));
    this.server = server;

    return await new Promise<string>((resolve, reject) => {
      const onError = (err: Error) => {
        this.server = undefined;
        reject(new Error(
          `Не удалось занять порт ${port} для приема кода авторизации. ` +
          `Возможно, запущен ReportChecker Studio или CLI. (${err.message})`,
        ));
      };
      server.once('error', onError);
      server.listen(port, '127.0.0.1', () => {
        server.removeListener('error', onError);
        resolve(`http://localhost:${port}/`);
      });
    });
  }

  /** Ждет код авторизации (или явной ошибки) с таймаутом. */
  async waitForCode(timeoutMs = 5 * 60 * 1000): Promise<string> {
    if (!this.server) throw new Error('Loopback-сервер не запущен');
    return await new Promise<string>((resolve, reject) => {
      const timer = setTimeout(() => {
        cleanup();
        reject(new Error('Время ожидания авторизации истекло'));
      }, timeoutMs);

      const onCode = (code: string) => {
        cleanup();
        resolve(code);
      };
      this.onCode = onCode;
      this.onError = (message: string) => {
        cleanup();
        reject(new Error(message));
      };

      function cleanup() {
        clearTimeout(timer);
      }
    });
  }

  private onCode?: (code: string) => void;
  private onError?: (message: string) => void;

  private handle(req: http.IncomingMessage, res: http.ServerResponse): void {
    const url = new URL(req.url ?? '/', `http://localhost:${this.port}`);
    const error = url.searchParams.get('error_description') ?? url.searchParams.get('error');
    const code = url.searchParams.get('code');

    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(
      '<html><head><meta charset="utf-8"><title>ReportChecker</title></head>' +
      '<body style="font-family: sans-serif; text-align: center; padding-top: 3em;">' +
      (code || error
        ? '<h2>Код авторизации получен</h2><p>Закройте эту страницу и вернитесь в VS Code.</p>'
        : '<h2>ReportChecker</h2><p>Ожидание авторизации…</p>') +
      '</body></html>',
    );

    if (error) {
      this.onError?.(`Авторизация не выполнена: ${error}`);
    } else if (code) {
      this.onCode?.(code);
    }
  }

  async stop(): Promise<void> {
    const server = this.server;
    this.server = undefined;
    this.onCode = undefined;
    this.onError = undefined;
    if (!server) return;
    await new Promise<void>((resolve) => server.close(() => resolve()));
  }

  /** redirect_uri, пригодный для передачи на сервер авторизации (с учетом проброса портов VS Code). */
  async resolveRedirectUri(port: number): Promise<string> {
    // env.asExternalUri корректен для localhost-редиректов и удаленных сценариев;
    // для локального запуска возвращает порт как есть.
    const uri = await vscode.env.asExternalUri(vscode.Uri.parse(`http://localhost:${port}`));
    return `${uri.scheme}://${uri.authority}/`;
  }
}
