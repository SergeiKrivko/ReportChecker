import * as vscode from 'vscode';
import type { FormatProvider } from '../formats/formatProvider';
import { allFormatProviders } from '../formats/formatProvider';

export interface WorkspaceLink {
  reportId: string;
  entryFile: string;
  format: string;
  reportName?: string;
  sourceProvider?: string;
}

/**
 * Связь «рабочая область ↔ отчет на сервере» (workspaceState) + clientId машины (globalState).
 */
export class LinkService {
  private static readonly LINK_KEY = 'reportchecker.link';
  private static readonly CLIENT_ID_KEY = 'reportchecker.clientId';

  constructor(
    private readonly context: vscode.ExtensionContext,
  ) {
  }

  async getLink(): Promise<WorkspaceLink | undefined> {
    const raw = this.context.workspaceState.get<WorkspaceLink>(LinkService.LINK_KEY);
    return raw?.reportId ? raw : undefined;
  }

  async setLink(link: WorkspaceLink): Promise<void> {
    await this.context.workspaceState.update(LinkService.LINK_KEY, link);
  }

  async clearLink(): Promise<void> {
    await this.context.workspaceState.update(LinkService.LINK_KEY, undefined);
  }

  async getClientId(): Promise<string> {
    let id = this.context.globalState.get<string>(LinkService.CLIENT_ID_KEY);
    if (!id) {
      id = cryptoRandomGuid();
      await this.context.globalState.update(LinkService.CLIENT_ID_KEY, id);
    }
    return id;
  }

  /** Провайдер формата для связанного отчета. */
  static providerFor(link: WorkspaceLink | undefined): FormatProvider | undefined {
    if (!link) return undefined;
    return allFormatProviders().find((p) => p.key === link.format);
  }
}

/** GUID без внешних зависимостей (crypto.randomUUID есть в Node 14.17+/19+). */
function cryptoRandomGuid(): string {
  const c = globalThis.crypto;
  if (c?.randomUUID) return c.randomUUID();
  const bytes = new Uint8Array(16);
  c.getRandomValues(bytes);
  bytes[6] = (bytes[6]! & 0x0f) | 0x40;
  bytes[8] = (bytes[8]! & 0x3f) | 0x80;
  const hex = [...bytes].map((b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
