import * as vscode from 'vscode';
import type { Diff, diff_match_patch } from 'diff-match-patch';
import type { Comment, Issue } from '../api/types';
import { EMPTY_USER_ID } from '../api/types';
import { FileIssue } from '../formats/formatProvider';
import { CommentService } from '../services/commentService';
import { IssueService } from '../services/issueService';
import { PatchService } from '../services/patchService';
import { log } from '../log';

// diff-match-patch не имеет types в основном пакете — типы из @types/diff-match-patch,
// сама библиотека подключается лениво (require в рантайме), чтобы не грузить её при старте.
type DmpConstructor = typeof diff_match_patch;
const DIFF_DELETE = -1;
const DIFF_INSERT = 1;
const DIFF_EQUAL = 0;
// Подложки из прошлой версии (проверено в темах VS Code, rgba в комментариях не рендерится)
const DEL_BG = '#f14c4c33';
const INS_BG = '#89d18533';
let dmpInstance: InstanceType<DmpConstructor> | undefined;

function getDiffMatchPatch(): InstanceType<DmpConstructor> {
  if (!dmpInstance) {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const Dmp = require('diff-match-patch') as DmpConstructor;
    dmpInstance = new Dmp();
  }
  return dmpInstance;
}

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
      // Порядок важен: сначала contextValue, потом comments — VS Code перечитывает
      // контекстный ключ commentThread только при обновлении comments,
      // отдельное присваивание contextValue событий не поднимает
      existing.contextValue = this.threadContext(fi.issue);
      existing.comments = this.toVsCodeComments(fi.issue);
      existing.state = this.threadState(fi.issue);
      return existing;
    }
    const uri = vscode.Uri.file(fi.position!.path);
    const line = Math.max(0, fi.position!.line - 1);
    const range = new vscode.Range(line, 0, line, 0);
    const thread = this.controller.createCommentThread(uri, range, this.toVsCodeComments(fi.issue));
    thread.canReply = true;
    thread.collapsibleState = vscode.CommentThreadCollapsibleState.Collapsed;
    thread.label = fi.issue.title ?? 'Ошибка';
    thread.contextValue = this.threadContext(fi.issue);
    thread.state = this.threadState(fi.issue);
    this.threads.set(fi.issue.id, thread);
    return thread;
  }

  /**
   * Контекст треда по статусу ошибки — им управляется видимость кнопок
   * «Исправлено»/«Закрыть» (только для открытых) и «Открыть заново» (для закрытых/исправленных).
   * В when-клаузах manifest используется ключ `commentThread` (не `thread`!).
   */
  private threadContext(issue: Issue): string {
    return issue.status === 'Open' || issue.status === 'InProgress'
      ? 'rc-thread-open'
      : 'rc-thread-resolved';
  }

  /**
   * state треда: закрытые/исправленные ошибки помечаем Resolved — VS Code
   * сам отрисовывает такой маркер как «разрешенный» (галочка), что визуально
   * согласуется со списком ошибок. Своей иконки у треда в стабильном API нет.
   */
  private threadState(issue: Issue): vscode.CommentThreadState {
    return issue.status === 'Open' || issue.status === 'InProgress'
      ? vscode.CommentThreadState.Unresolved
      : vscode.CommentThreadState.Resolved;
  }

  /** Обновление команды треда; keepUnread — не отмечать прочитанным (кнопка «Отметить непрочитанным»). */
  updateIssue(issue: Issue, keepUnread = false): void {
    const thread = this.threads.get(issue.id);
    if (thread) {
      thread.contextValue = this.threadContext(issue);
      thread.comments = this.toVsCodeComments(issue);
      thread.state = this.threadState(issue);
    }
    if (!keepUnread) void this.issueService.markIssueRead(issue);
  }

  /** Открыть тред ошибки: раскрыть и показать в редакторе. */
  async openIssue(fi: FileIssue): Promise<void> {
    if (!fi.position) {
      const choice = await vscode.window.showInformationMessage(
        'Для этой ошибки не удалось определить место в файлах. Открыть в браузере?',
        'Открыть в браузере',
      );
      if (choice === 'Открыть в браузере') {
        await vscode.commands.executeCommand('reportchecker.openInWeb', fi.issue);
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
  }

  /**
   * Дифф патча в markdown: строки по номеру; целые строки Add — зелёным, Delete — красным
   * (в web так же), для Modify — посимвольный diff внутри строки, как в web-версии.
   * Цвета — из прошлой версии расширения (проверены в темах VS Code).
   */
  private patchMarkdown(comment: Comment): string {
    const lines = [...(comment.patch?.lines ?? [])].sort((a, b) => a.number - b.number);
    if (lines.length === 0) return '';

    const rows = lines.map((l) => {
      const num = `<span style="color:#808080;">${l.number}</span>`;
      switch (l.type) {
        case 'Add':
          return `<span style="background-color:${INS_BG};">${num} ${escapeHtml(singleLine(l.content ?? ''))}</span>`;
        case 'Delete':
          return `<span style="background-color:${DEL_BG};">${num} ${escapeHtml(singleLine(l.previousContent ?? ''))}</span>`;
        case 'Modify':
          return `${num} ${this.charDiffHtml(l.previousContent ?? '', l.content ?? '')}`;
        default:
          return `${num} ${escapeHtml(singleLine(l.content ?? ''))}`;
      }
    });
    // Разделяем пустой строкой (отдельные абзацы): <br> рендерер комментариев
    // VS Code не гарантирует, а параграфы работают в любом markdown-контексте
    return rows.join('\n\n');
  }

  /**
   * Посимвольный diff строки (аналог DiffHtmlPipe из web-версии на diff-match-patch).
   * Пробелы на краях окрашенных блоков выносятся наружу: подсветка не должна
   * «съедать» их визуально, а markdown-разметка (~~ ~~) не должна глотать пробелы.
   */
  private charDiffHtml(previous: string, current: string): string {
    const dmp = getDiffMatchPatch();
    const diffs = dmp.diff_main(previous, current);
    dmp.diff_cleanupSemantic(diffs);
    let html = '';
    for (const [op, data] of diffs) {
      if (op === DIFF_EQUAL) {
        html += escapeHtml(flatten(data));
        continue;
      }
      // Крайние пробелы — вне окрашенного спана: подсветка не «съедает» их визуально,
      // и markdown-разметка (~~ ~~) не глотает пробелы на границах блоков
      const leading = data.match(/^\s+/)?.[0] ?? '';
      const trailing = data.match(/\s+$/)?.[0] ?? '';
      const core = data.slice(leading.length, data.length - trailing.length);
      if (core.length > 0) {
        const coreHtml =
          op === DIFF_DELETE
            ? `<span style="background-color:${DEL_BG};">~~${escapeHtml(flatten(core))}~~</span>`
            : `<span style="background-color:${INS_BG};">${escapeHtml(flatten(core))}</span>`;
        html += escapeHtml(flatten(leading)) + coreHtml + escapeHtml(flatten(trailing));
      } else if (data.length > 0) {
        html += escapeHtml(flatten(data));
      }
    }
    return html;
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
        const patchStatus = c.patch.status;
        // Кнопки под предложением — command-ссылки в теле комментария; видны,
        // только пока решение не принято (Accepted/Rejected/Applied/Failed — скрыты)
        const openPatch =
          !patchStatus || patchStatus === 'Pending' || patchStatus === 'InProgress';
        const patchArgs = encodeURIComponent(JSON.stringify([issue, c]));
        const diff = this.patchMarkdown(c);
        if (openPatch) {
          body.appendMarkdown(
            `**$(wand) Предложение исправления**\n\n` +
            `${diff}\n\n` +
            `[$(check) Применить](command:reportchecker.applyPatch?${patchArgs} "Применить исправление к файлу") ` +
            `· ` +
            `[$(close) Отклонить](command:reportchecker.rejectPatch?${patchArgs} "Отклонить исправление")\n\n`,
          );
        } else {
          body.appendMarkdown(
            `**$(wand) Предложение исправления** _(${patchStatusRu(patchStatus)})_\n\n` +
            `${diff}\n\n`,
          );
        }
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

  /** Найти Issue по треду (сначала по объекту, затем по uri+диапазону из-за маршалинга). */
  issueForThread(thread: vscode.CommentThread): Issue | undefined {
    for (const [id, t] of this.threads) {
      if (t === thread) {
        const fi = this.issueService.getFileIssues().find((e) => e.issue.id === id);
        return fi?.issue;
      }
    }
    // Меню треда могло прислать копию объекта (маршелинг) — совпадаем по uri и диапазону
    const uriKey = thread.uri?.toString();
    if (uriKey) {
      for (const [id, t] of this.threads) {
        if (
          t.uri.toString() === uriKey &&
          t.range && thread.range &&
          t.range.start.line === thread.range.start.line &&
          t.range.start.character === thread.range.start.character &&
          t.range.end.line === thread.range.end.line &&
          t.range.end.character === thread.range.end.character
        ) {
          log(`[threads] тред найден по uri/range (объекты не совпали — маршалинг)`);
          const fi = this.issueService.getFileIssues().find((e) => e.issue.id === id);
          return fi?.issue;
        }
      }
    }
    log(
      `[threads] тред не найден: uri=${uriKey ?? '<нет>'}, ` +
      `range=${thread.range ? `${thread.range.start.line + 1}:${thread.range.end.line + 1}` : '<нет>'}, ` +
      `своих тредов: ${this.threads.size}`,
    );
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

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/** Многострочный контент патча укладывается в одну строку визуального диффа. */
function singleLine(s: string): string {
  return flatten(s).trim();
}

/** Переводы строк — в видимый маркер, но края не трогаем (пробелы значимы в диффе). */
function flatten(s: string): string {
  return s.replace(/\s*\n\s*/g, ' ⏎ ');
}
export function patchStatusRu(status: string | null | undefined): string {
  switch (status) {
    case 'Pending':
      return 'ожидает решения';
    case 'InProgress':
      return 'рассматривается';
    case 'Completed':
      return 'подготовлено';
    case 'Failed':
      return 'не удалось применить';
    case 'Accepted':
      return 'принято';
    case 'Rejected':
      return 'отклонено';
    case 'Applied':
      return 'применено';
    default:
      return status ?? '';
  }
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
