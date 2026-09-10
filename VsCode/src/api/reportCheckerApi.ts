import { HttpClient } from './httpClient';
import type {
  Check,
  Comment,
  CreateCheckSchema,
  CreateCommentSchema,
  CreateReportSchema,
  Issue,
  IssueStatus,
  MarkReadSchema,
  PatchStatus,
  Report,
  UpdateCommentSchema,
  UpdatePatchSchema,
  UploadFileResponse,
  UserInfo,
} from './types';
import { EMPTY_USER_ID } from './types';

/** REST-клиент ReportChecker (только используемые плагином эндпоинты). */
export class ReportCheckerApi {
  constructor(
    private readonly baseUrl: string,
    private readonly http: HttpClient,
  ) {
  }

  // --- Reports ---

  async getAllReports(): Promise<Report[]> {
    return await this.http.getJson<Report[]>(`${this.baseUrl}/api/v1/reports`);
  }

  async getReport(reportId: string): Promise<Report> {
    return await this.http.getJson<Report>(`${this.baseUrl}/api/v1/reports/${reportId}`);
  }

  async createReport(schema: CreateReportSchema): Promise<string> {
    return await this.http.postJson<string>(`${this.baseUrl}/api/v1/reports`, schema);
  }

  async testSource(provider: string, source: object): Promise<{ status: string; format?: string | null }> {
    return await this.http.postJson(`${this.baseUrl}/api/v1/reports/test-source`, {
      provider,
      source,
    });
  }

  // --- Checks ---

  async getLatestCheck(reportId: string): Promise<Check> {
    return await this.http.getJson<Check>(`${this.baseUrl}/api/v1/reports/${reportId}/checks/latest`);
  }

  async createCheck(reportId: string, schema: CreateCheckSchema): Promise<string> {
    return await this.http.postJson<string>(`${this.baseUrl}/api/v1/reports/${reportId}/checks`, schema);
  }

  // --- Files ---

  async uploadFile(bucket: 'Default' | 'Local', fileName: string, data: Uint8Array): Promise<UploadFileResponse> {
    const form = new FormData();
    const copy = new Uint8Array(data.length);
    copy.set(data);
    form.append('file', new Blob([copy], { type: 'application/zip' }), fileName);
    return await this.http.postForm<UploadFileResponse>(
      `${this.baseUrl}/api/v1/files?bucket=${bucket}`,
      form,
    );
  }

  // --- Issues ---

  async getAllIssues(reportId: string): Promise<Issue[]> {
    return await this.http.getJson<Issue[]>(`${this.baseUrl}/api/v1/reports/${reportId}/issues`);
  }

  // --- Comments ---

  async getComments(reportId: string, issueId: string): Promise<Comment[]> {
    return await this.http.getJson<Comment[]>(
      `${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments`,
    );
  }

  async createComment(reportId: string, issueId: string, schema: CreateCommentSchema): Promise<string> {
    return await this.http.postJson<string>(
      `${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments`,
      schema,
    );
  }

  async updateComment(reportId: string, issueId: string, commentId: string, schema: UpdateCommentSchema): Promise<void> {
    await this.http.putJson(
      `${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments/${commentId}`,
      schema,
    );
  }

  async deleteComment(reportId: string, issueId: string, commentId: string): Promise<void> {
    await this.http.delete(`${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments/${commentId}`);
  }

  async markRead(reportId: string, issueId: string, schema: MarkReadSchema): Promise<void> {
    await this.http.postJson(
      `${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments/read`,
      schema,
    );
  }

  async setPatchStatus(reportId: string, issueId: string, commentId: string, status: PatchStatus): Promise<void> {
    await this.http.putJson(
      `${this.baseUrl}/api/v1/reports/${reportId}/issues/${issueId}/comments/${commentId}/patch`,
      { status } satisfies UpdatePatchSchema,
    );
  }

  /** Статусные комментарии (Open/Closed/Fixed) создаются обычным POST с полем status. */
  async createStatusComment(reportId: string, issueId: string, status: IssueStatus): Promise<string> {
    return await this.createComment(reportId, issueId, { status });
  }

  // --- User ---

  async getUserInfo(): Promise<UserInfo> {
    // /userinfo доступен на сервере авторизации; здесь нет своего эндпоинта пользователя.
    throw new Error('Use AuthApi.getUserInfo');
  }
}

export { EMPTY_USER_ID };
