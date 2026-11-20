export interface YandexDiskConnectionEntity {
  connected: boolean;
  login?: string;
}

export interface YandexDiskResourceEntity {
  name: string;
  path: string;
  type: string;
  size?: number;
  md5?: string;
  modified?: string;
}

export interface YandexDiskFolderEntity {
  path: string;
  limit: number;
  offset: number;
  total: number;
  items: YandexDiskResourceEntity[];
}

export interface YandexDiskSyncResultEntity {
  created: boolean;
  checkId?: string;
  reason?: string;
}

export const SUPPORTED_YANDEX_DISK_EXTENSIONS = ['.tex', '.docx', '.pdf', '.zip'];

export const isSupportedYandexDiskFile = (name: string): boolean =>
  SUPPORTED_YANDEX_DISK_EXTENSIONS.some(extension => name.toLowerCase().endsWith(extension));
