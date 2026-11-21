export interface GoogleDriveConnectionEntity {
  /** Привязан ли Google-аккаунт в Avalux Auth. */
  hasAccount: boolean;
  /** Выдан ли токен доступа к Диску. */
  connected: boolean;
  login?: string;
}

export interface GoogleDriveResourceEntity {
  id: string;
  name: string;
  type: 'dir' | 'file';
  mimeType: string;
  size?: number;
  modified?: string;
}

export interface GoogleDriveFolderEntity {
  folderId: string;
  pageSize: number;
  nextPageToken?: string;
  items: GoogleDriveResourceEntity[];
}

export interface GoogleDriveSyncResultEntity {
  created: boolean;
  checkId?: string;
  reason?: string;
}

export const SUPPORTED_GOOGLE_DRIVE_EXTENSIONS = ['.tex', '.docx', '.pdf', '.zip'];

export const isSupportedGoogleDriveFile = (name: string): boolean =>
  SUPPORTED_GOOGLE_DRIVE_EXTENSIONS.some(extension => name.toLowerCase().endsWith(extension));
