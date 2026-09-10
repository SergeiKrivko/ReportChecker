import * as vscode from 'vscode';
import type { Comment, Issue, IssueStatus, PatchStatus } from '../api/types';
import { EMPTY_USER_ID } from '../api/types';
import { LinkService } from './linkService';
import { ReportCheckerApi } from '../api/reportCheckerApi';

export interface ThreadActionContext {
  issue: Issue;
}

/**
 * Комментарии: загрузка, ответы, смена статуса, patch-статусы и поллинг
 * ответа ИИ (аналог Studio.CommentsService).
 */
export class CommentService implements vscode.Disposable {
  private readonly pollTimers = new Map<string, NodeJS.Timeout>();

  constructor(
    private readonly api: ReportCheckerApi,
    private readonly linkService: LinkService,
    private readonly onCommentsChanged: (issue: Issue, comments: Comment[]) => void,
  ) {
  }

  async loadComments(issue: Issue): Promise<Comment[]> {
    const link = await this.linkService.getLink();
    if (!link) return [];
    try {
      const comments: Comment[] = await this.api.getComments(link.reportId, issue.id);
      const sorted = [...comments].sort(byCreatedAt);
      issue.comments = sorted;
      this.onCommentsChanged(issue, sorted);
      return sorted;
    } catch (e) {
      void vscode.window.showWarningMessage(
        `Не удалось загрузить комментарии: ${e instanceof Error ? e.message : String(e)}`,
      );
      return [];
    }
  }

  /** Ответ текстом (POST комментария) + запуск поллинга ответа ИИ. */
  async reply(issue: Issue, content: string): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link) return;
    try {
      await this.api.createComment(link.reportId, issue.id, { content });
      await this.loadComments(issue);
      await this.pollAiReply(link.reportId, issue);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось отправить комментарий: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  /** Смена статуса ошибки статусным комментарием (Open/Closed/Fixed). */
  async setStatus(issue: Issue, status: IssueStatus): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link) return;
    try {
      await this.api.createStatusComment(link.reportId, issue.id, status);
      await this.loadComments(issue);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось сменить статус: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  async editComment(issue: Issue, comment: Comment, content: string): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link || !comment.id) return;
    try {
      await this.api.updateComment(link.reportId, issue.id, comment.id, { content });
      await this.loadComments(issue);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось изменить комментарий: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  async deleteComment(issue: Issue, comment: Comment): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link || !comment.id) return;
    try {
      await this.api.deleteComment(link.reportId, issue.id, comment.id);
      await this.loadComments(issue);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось удалить комментарий: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  /** Принять/Отклонить/Применить патч. */
  async setPatchStatus(issue: Issue, comment: Comment, status: PatchStatus): Promise<void> {
    const link = await this.linkService.getLink();
    if (!link || !comment.id || !comment.patch) return;
    try {
      await this.api.setPatchStatus(link.reportId, issue.id, comment.id, status);
      comment.patch.status = status;
      this.onCommentsChanged(issue, issue.comments ?? []);
      if (status !== 'Rejected') await this.loadComments(issue);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось обновить статус исправления: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  }

  /** Поллинг ответа ИИ: комментарий с userId=000…0 до прогресса Completed/Failed. */
  private async pollAiReply(reportId: string, issue: Issue): Promise<void> {
    const key = issue.id;
    const existing = this.pollTimers.get(key);
    if (existing) clearTimeout(existing);

    const attempt = async (): Promise<void> => {
      try {
        const comments: Comment[] = await this.api.getComments(reportId, issue.id);
        const aiComment: Comment | undefined = comments
          .filter((c) => (c.userId ?? '').toLowerCase() === EMPTY_USER_ID)
          .sort(byCreatedAt)
          .at(-1);
        if (!aiComment) return;
        if (aiComment.progressStatus === 'Completed' || aiComment.progressStatus === 'Failed') {
          issue.comments = [...comments].sort(byCreatedAt);
          this.onCommentsChanged(issue, issue.comments!);
          this.pollTimers.delete(key);
          return;
        }
        const timer = setTimeout(() => void attempt(), 1500);
        this.pollTimers.set(key, timer);
      } catch {
        this.pollTimers.delete(key);
      }
    };

    const first = setTimeout(() => void attempt(), 1500);
    this.pollTimers.set(key, first);
  }

  dispose(): void {
    for (const timer of this.pollTimers.values()) clearTimeout(timer);
    this.pollTimers.clear();
  }
}

function byCreatedAt(a: Comment, b: Comment): number {
  const ta = a.createdAt ? Date.parse(a.createdAt) : 0;
  const tb = b.createdAt ? Date.parse(b.createdAt) : 0;
  return ta - tb;
}
