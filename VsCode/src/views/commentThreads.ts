import * as vscode from 'vscode';
import type { Comment, Issue } from '../api/types';
import { EMPTY_USER_ID } from '../api/types';
import { FileIssue } from '../formats/formatProvider';
import { CommentService } from '../services/commentService';
import { IssueService } from '../services/issueService';
import { PatchService } from '../services/patchService';

/**
 * Треды комментариев в редакторе (vscode.CommentController):
 * тред создается на строке каждой привязанной ошибки, ответы и статусы — как в web.
 */
export class CommentThreads implements vscode.Disposable {
  readonly controller: vscode.CommentController;
  private readonly threads = new Map<string, vscode.CommentThread>();
  private currentUserName = 'Вы';
  private readonly disposables: vscode.Disposable[] = [];

  constructor(
    private readonly commentService: CommentService,
    private readonly issueService: IssueService,
    private readonly patchService: PatchService,
  ) {
    this.controller = vscode.comments.createCommentController('reportchecker', 'ReportChecker');
    this.controller.options = {
      placeHolder: 'Ответить или задать вопрос…',
      prompt: 'Комментарий будет отправлен в ReportChecker',
    };
    this.disposables.push(this.controller);
  }

  async setUserName(name: string | undefined): Promise<void> {
    if (name) this.currentUserName = name;
  }

  /** Полностью перестраивает треды из текущего списка FileIssue. */
  sync(issues: readonly FileIssue[]): void {
    const alive = new Set<string>();
    for (const fi of issues) {
      if (!fi.position) continue;
      alive.add(fi.issue.id);
      this.ensureThread(fi);
    }
    for (const [id, thread] of [...this.threads]) {
      if (!alive.has(id)) {
        thread.dispose();
        this.threads.delete(id);
      }
    }
  }

  private ensureThread(fi: FileIssue): vscode.CommentThread {
    const existing = this.threads.get(fi.issue.id);
    if (existing) {
      existing.comments = this.toVsCodeComments(fi.issue);
      return existing;
    }
    const uri = vscode.Uri.file(fi.position!.path);
    const line = Math.max(0, fi.position!.line - 1);
    const range = new vscode.Range(line, 0, line, 0);
    const thread = this.controller.createCommentThread(uri, range, this.toVsCodeComments(fi.issue));
    thread.canReply = true;
    thread.collapsibleState = vscode.CommentThreadCollapsibleState.Collapsed;
    thread.label = fi.issue.title ?? 'Ошибка';
    thread.contextValue = 'rc-thread';
    this.threads.set(fi.issue.id, thread);
    return thread;
  }

  /** Обновление комментариев конкретной ошибки (после ответа/статуса/поллинга ИИ). */
  updateIssue(issue: Issue): void {
    const thread = this.threads.get(issue.id);
    if (thread) {
      thread.comments = this.toVsCodeComments(issue);
    }
    void this.issueService.markIssueRead(issue);
  }

  /** Открыть тред ошибки: раскрыть и показать в редакторе. */
  async openIssue(fi: FileIssue): Promise<void> {
    if (!fi.position) {
      const choice = await vscode.window.showInformationMessage(
        'Для этой ошибки не удалось определить место в файлах. Открыть в браузере?',
        'Открыть в браузере',
      );
      if (choice === 'Открыть в браузере') {
        await vscode.commands.executeCommand('reportchecker.openInWeb');
      }
      return;
    }

    const uri = vscode.Uri.file(fi.position.path);
    const document = await vscode.workspace.openTextDocument(uri);
    const editor = await vscode.window.showTextDocument(document, { preview: false });
    const lineIndex = Math.min(Math.max(fi.position.line - 1, 0), document.lineCount - 1);
    const range = document.lineAt(lineIndex).range;
    editor.revealRange(range, vscode.TextEditorRevealType.InCenter);
    editor.selection = new vscode.Selection(range.start, range.end);

    const thread = this.ensureThread(fi);
    thread.collapsibleState = vscode.CommentThreadCollapsibleState.Expanded;
    void this.commentService.loadComments(fi.issue).then((comments) => {
      thread.comments = this.toVsCodeComments(fi.issue);
      void comments;
    });
    void this.issueService.markIssueRead(fi.issue);
  }

  /** Рендер диффа патча в markdown. */
  private patchMarkdown(comment: Comment): string {
    const lines = comment.patch?.lines ?? [];
    if (lines.length === 0) return '';
    const parts = lines.map((l) => {
      const content = (l.content ?? '').replace(/\n/g, ' ');
      switch (l.type) {
        case 'Add':
          return `- ${content}`;
        case 'Modify':
          return `~ ${content}`;
        default:
          return `~~${(l.previousContent ?? '').replace(/\n/g, ' ')}~~`;
      }
    });
    return parts.join('\n');
  }

  private toVsCodeComments(issue: Issue): vscode.Comment[] {
    const result: vscode.Comment[] = [];
    const statusOf = issue.status;
    for (const c of issue.comments ?? []) {
      if (c.deletedAt) continue;
      const isAi = (c.userId ?? '').toLowerCase() === EMPTY_USER_ID;
      const authorName = isAi ? 'ReportChecker AI' : this.currentUserName;
      const body = new vscode.MarkdownString();
      body.isTrusted = true;
      body.supportThemeIcons = true;
      const meta: string[] = [];
      if (c.createdAt) meta.push(formatDate(c.createdAt));
      if (c.content) {
        body.appendMarkdown(c.content + '\n\n');
      }
      if (c.patch) {
        body.appendMarkdown(`**$(wand) Предложение исправления**\n\n\`\`\`\n${this.patchMarkdown(c)}\n\`\`\`\n\n`);
      }
      if (c.status) meta.push(`статус: ${issueStatusRu(c.status)}`);
      if (meta.length > 0) body.appendMarkdown(`_${meta.join(' · ')}_`);
      if (c.progressStatus && c.progressStatus !== 'Completed') {
        body.appendMarkdown(`\n\n_$(sync~spin) ИИ отвечает…_`);
      }

      const modes: vscode.CommentMode[] = [];
      void modes;

      const vsComment: vscode.Comment & { rcComment?: Comment; rcIssue?: Issue; contextValue?: string } = {
        body,
        mode: vscode.CommentMode.Preview,
        author: { name: authorName },
        label: isAi ? 'ИИ' : 'Вы',
        timestamp: c.createdAt ? new Date(c.createdAt) : undefined,
        rcComment: c,
        rcIssue: issue,
        contextValue: c.patch
          ? 'rc-patch'
          : !isAi && c.id
            ? 'rc-own'
            : undefined,
      };
      result.push(vsComment);
      void statusOf;
    }
    return result;
  }

  /** Достать сохраненные контексты из vscode.Comment (используются в командах). */
  static extract(comment: unknown): { rcComment?: Comment; rcIssue?: Issue } {
    if (comment && typeof comment === 'object') {
      const c = comment as { rcComment?: Comment; rcIssue?: Issue };
      return { rcComment: c.rcComment, rcIssue: c.rcIssue };
    }
    return {};
  }

  static extractReply(commentReply: unknown): { thread?: vscode.CommentThread; text?: string } {
    if (commentReply && typeof commentReply === 'object' && 'thread' in (commentReply as object)) {
      const r = commentReply as { thread: vscode.CommentThread; text: string };
      return { thread: r.thread, text: r.text };
    }
    return {};
  }

  /** Найти Issue по треду (thread.uri + диапазон). */
  issueForThread(thread: vscode.CommentThread): Issue | undefined {
    for (const [id, t] of this.threads) {
      if (t === thread) {
        const fi = this.issueService.getFileIssues().find((e) => e.issue.id === id);
        return fi?.issue;
      }
    }
    return undefined;
  }

  dispose(): void {
    for (const d of this.disposables) d.dispose();
  }
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function issueStatusRu(status: string): string {
  switch (status) {
    case 'Open':
      return 'открыта';
    case 'InProgress':
      return 'в работе';
    case 'Closed':
      return 'закрыта';
    case 'Fixed':
      return 'исправлена';
    default:
      return status;
  }
}
