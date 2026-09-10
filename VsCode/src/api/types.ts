// DTO и енумы в точности по swagger-контракту https://api.reportchecker.ru/swagger/v1/swagger.json

export type ProgressStatus =
  | 'Queued'
  | 'InProgress'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'CancellationRequested';

export type IssueStatus = 'Open' | 'InProgress' | 'Closed' | 'Fixed';

export type PatchStatus =
  | 'Pending'
  | 'InProgress'
  | 'Completed'
  | 'Failed'
  | 'Accepted'
  | 'Rejected'
  | 'Applied';

export type PatchLineType = 'Add' | 'Delete' | 'Modify';

export type FileBucket = 'Default' | 'Local';

export interface Check {
  id: string;
  reportId: string;
  userId: string;
  name?: string | null;
  createdAt?: string | null;
  status?: ProgressStatus | null;
}

export interface Report {
  id: string;
  ownerId: string;
  name?: string | null;
  sourceProvider?: string | null;
  format?: string | null;
  llmModelId?: string | null;
  imageProcessingMode?: string;
  createdAt?: string | null;
  deletedAt?: string | null;
  updatedAt?: string | null;
  source?: ReportSourceUnion | null;
  issueCount?: Record<string, number> | null;
}

export interface ReportSourceUnion {
  gitHub?: GitHubReportSource | null;
  file?: FileReportSource | null;
  local?: LocalReportSource | null;
}

export interface LocalReportSource {
  initialFileId: string;
  entryFilePath?: string | null;
  clientId?: string;
  clientMachineName?: string | null;
}

export interface FileReportSource {
  initialFileId: string;
  entryFilePath?: string | null;
}

export interface GitHubReportSource {
  repositoryId: number;
  branch?: string | null;
  path?: string | null;
}

export interface CreateReportSchema {
  name: string;
  format: string;
  sourceProvider: string;
  source: ReportSourceUnion;
  llmModelId?: string | null;
  imageProcessingMode?: 'Disable' | 'Auto' | 'LowDetail' | 'HighDetail';
}

export interface CreateCheckSchema {
  source?: CheckSourceUnion | null;
  name?: string | null;
}

export interface CheckSourceUnion {
  id?: string | null;
  gitHub?: { commitHash?: string | null } | null;
  file?: { fileName?: string | null; createdAt?: string; deletedAt?: string | null } | null;
  local?: { fileName?: string | null; createdAt?: string; deletedAt?: string | null } | null;
}

export interface Issue {
  id: string;
  checkId: string;
  title?: string | null;
  status?: IssueStatus | null;
  priority?: number;
  chapter?: string | null;
  line?: number | null;
  comments?: Comment[] | null;
}

export interface Comment {
  id?: string | null;
  issueId?: string | null;
  userId?: string | null;
  content?: string | null;
  status?: IssueStatus | null;
  progressStatus?: ProgressStatus | null;
  isRead?: boolean | null;
  patch?: Patch | null;
  createdAt?: string | null;
  modifiedAt?: string | null;
  deletedAt?: string | null;
}

export interface Patch {
  id: string;
  commentId: string;
  status?: PatchStatus | null;
  lines?: PatchLine[] | null;
  createdAt?: string | null;
}

export interface PatchLine {
  number: number;
  content?: string | null;
  previousContent?: string | null;
  type?: PatchLineType;
}

export interface CreateCommentSchema {
  content?: string | null;
  status?: IssueStatus;
}

export interface UpdateCommentSchema {
  content: string;
}

export interface MarkReadSchema {
  isRead: boolean;
  commentIds?: string[];
}

export interface UpdatePatchSchema {
  status: PatchStatus;
}

export interface UploadFileResponse {
  id: string;
  fileName?: string | null;
}

export interface UserInfo {
  id: string;
  email?: string | null;
  accounts?: UserAccount[];
}

export interface UserAccount {
  id?: string;
  login?: string;
  name?: string;
  provider?: string;
  avatarUrl?: string;
}

export interface UserCredentials {
  accessToken: string;
  refreshToken: string;
  /** Срок жизни access-токена в секундах */
  expiresIn?: number;
  email?: string | null;
  /** Момент истечения, вычисляется при сохранении */
  expiresAt?: number;
}

export const EMPTY_USER_ID = '00000000-0000-0000-0000-000000000000';

export function isTerminalCheckStatus(status: ProgressStatus | null | undefined): boolean {
  return (
    status === 'Completed' || status === 'Failed' || status === 'Cancelled'
  );
}
