import * as path from 'path';
import * as vscode from 'vscode';
import { registerFormatProvider } from './formats/formatProvider';
import { LatexFormatProvider } from './formats/latex/latexProvider';
import { HttpClient } from './api/httpClient';
import { ReportCheckerApi } from './api/reportCheckerApi';
import { AuthApi } from './api/authApi';
import type { Comment, Issue } from './api/types';
import { AuthService, createAuthApi } from './auth/authService';
import { onDidChangeSettings, readSettings, Settings } from './env';
import { AutoUploadService } from './services/autoUploadService';
import { CheckPhase, CheckService } from './services/checkService';
import { CommentService } from './services/commentService';
import { IssueService } from './services/issueService';
import { LinkService, WorkspaceLink } from './services/linkService';
import { PatchService } from './services/patchService';
import { CommentThreads } from './views/commentThreads';
import { IssueDecorations } from './views/decorations';
import { IssuesTreeDataProvider } from './views/issuesTree';
import { StatusBar } from './views/statusBar';
import { log, showLog } from './log';

export function activate(context: vscode.ExtensionContext): void {
  registerFormatProvider(new LatexFormatProvider());

  let settings = readSettings();

  const httpClient = new HttpClient(
    () => (authService ? authService.getAccessToken() : Promise.reject(new Error('not ready'))),
    () => (authService ? authService.onUnauthorized() : Promise.reject(new Error('not ready'))),
  );
  const authApi = createAuthApi(settings, httpClient);
  const api = new ReportCheckerApi(settings.apiBaseUrl, httpClient);

  const linkService = new LinkService(context);
  let authService: AuthService | undefined;
  let checkService: CheckService | undefined;
  let issueService: IssueService | undefined;
  let commentService: CommentService | undefined;
  let patchService: PatchService | undefined;
  let autoUpload: AutoUploadService | undefined;
  let tree: IssuesTreeDataProvider | undefined;
  let threads: CommentThreads | undefined;
  let decorations: IssueDecorations | undefined;
  let statusBar: StatusBar | undefined;

  const setContexts = async (): Promise<void> => {
    await vscode.commands.executeCommand('setContext', 'reportchecker.loggedIn', !!authService?.isLoggedIn());
    const link = await linkService.getLink();
    await vscode.commands.executeCommand('setContext', 'reportchecker.linked', !!link);
  };

  const reloadIssues = async (): Promise<void> => {
    if (!issueService || !threads || !decorations || !tree) return;
    const issues = await issueService.reload();
    threads.sync(issues);
    decorations.update(issues);
    tree.refresh();
    statusBar?.setUnread(issueService.unreadCount());
  };

  const refreshLatestCheck = async (): Promise<void> => {
    if (!checkService) return;
    await checkService.refreshLatestCheck();
    await reloadIssues();
  };

  const attachAutoUpload = async (): Promise<void> => {
    if (!autoUpload) return;
    await autoUpload.attach();
    statusBar?.setAutoUpload(settings.autoUpload && !autoUpload.isPaused());
  };

  // --- команды ---

  const loginCommand = vscode.commands.registerCommand('reportchecker.login', async () => {
    try {
      const ok = await authService!.login();
      if (ok) {
        await setContexts();
        const info = await authService!.getSessionInfo();
        await threads!.setUserName(info.userName);
        statusBar?.setLoggedIn(true);
        void vscode.window.showInformationMessage(
          `Вы вошли в ReportChecker${info.userName ? ` как ${info.userName}` : ''}`,
        );
        await refreshLatestCheck();
        await attachAutoUpload();
        tree?.refresh();
      }
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось войти: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  });

  const logoutCommand = vscode.commands.registerCommand('reportchecker.logout', async () => {
    await authService!.logout();
    await setContexts();
    statusBar?.setLoggedIn(false);
    tree?.refresh();
  });

  const requireAuth = async (): Promise<boolean> => {
    if (!authService!.isLoggedIn()) {
      const choice = await vscode.window.showInformationMessage(
        'Вы не авторизованы в ReportChecker. Войти сейчас?',
        'Войти',
      );
      if (choice === 'Войти') await vscode.commands.executeCommand('reportchecker.login');
      return authService!.isLoggedIn();
    }
    return true;
  };

  const sendForCheckCommand = vscode.commands.registerCommand('reportchecker.sendForCheck', async () => {
    if (!(await requireAuth())) return;
    const provider = getProviderForWorkspace();
    if (!provider) {
      void vscode.window.showErrorMessage(
        'Не найден поддерживаемый формат отчета (плагин пока поддерживает LaTeX: *.tex)',
      );
      return;
    }
    const entry = await checkService!.resolveEntryFile(provider, forceNewEntry());
    if (!entry) return;
    try {
      const link = await checkService!.createReport(provider, entry);
      await setContexts();
      await attachAutoUpload();
      void vscode.window.showInformationMessage(
        `Отчет «${path.basename(entry)}» отправлен на проверку`,
      );
      void link;
      await vscode.commands.executeCommand('reportchecker.refreshIssues');
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось отправить отчет: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  });

  const sendVersionCommand = vscode.commands.registerCommand('reportchecker.sendVersion', async () => {
    if (!(await requireAuth())) return;
    const link = await linkService.getLink();
    const provider = LinkService.providerFor(link);
    if (!provider) {
      void vscode.window.showWarningMessage('Формат отчета не поддерживается плагином');
      return;
    }
    const entry = await checkService!.resolveEntryFile(provider);
    if (!entry) return;
    try {
      await checkService!.uploadVersion(provider, entry);
      void vscode.window.setStatusBarMessage('ReportChecker: новая версия отправлена на проверку', 5000);
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось отправить версию: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  });

  const linkReportCommand = vscode.commands.registerCommand('reportchecker.linkReport', async () => {
    if (!(await requireAuth())) return;
    try {
      const reports = await api.getAllReports();
      if (reports.length === 0) {
        void vscode.window.showInformationMessage(
          'На сервере пока нет отчетов. Отправьте проект на проверку.',
        );
        return;
      }
      const items = reports
        .filter((r) => !r.deletedAt)
        .sort((a, b) => Date.parse(b.createdAt ?? '0') - Date.parse(a.createdAt ?? '0'))
        .map((r) => ({
          label: r.name ?? r.id,
          description: `${r.format ?? '?'} · ${r.sourceProvider ?? '?'}`,
          report: r,
        }));
      const picked = await vscode.window.showQuickPick(items, {
        title: 'Привязать отчет к рабочей области',
      });
      if (!picked) return;

      if (picked.report.format && picked.report.format !== 'Latex') {
        void vscode.window.showWarningMessage(
          `Формат отчета «${picked.report.format}» не поддерживается плагином (пока только LaTeX)`,
        );
      }
      if (picked.report.sourceProvider && picked.report.sourceProvider !== 'Local') {
        void vscode.window.showWarningMessage(
          `Источник отчета «${picked.report.sourceProvider}»: автоотправка версий будет недоступна`,
        );
      }
      const link: WorkspaceLink = {
        reportId: picked.report.id,
        entryFile: '',
        format: picked.report.format ?? 'Latex',
        reportName: picked.report.name ?? undefined,
        sourceProvider: picked.report.sourceProvider ?? undefined,
      };
      await linkService.setLink(link);
      // Сразу определяем и сохраняем главный файл (лучший кандидат по \include),
      // чтобы привязка не зависела от эвристики при каждой перезагрузке ошибок
      try {
        const provider = LinkService.providerFor(link);
        const folder = vscode.workspace.workspaceFolders?.[0];
        if (provider?.pickBestEntry && folder) {
          const matches = await vscode.workspace.findFiles(provider.watchGlob, '**/node_modules/**', 200);
          const contents = new Map<string, string>();
          for (const m of matches) {
            try {
              const doc = await vscode.workspace.openTextDocument(m);
              contents.set(m.fsPath, doc.getText().slice(0, 20000));
            } catch {
              contents.set(m.fsPath, '');
            }
          }
          const best = provider.pickBestEntry(matches.map((m) => m.fsPath), contents);
          if (best) {
            link.entryFile = path.relative(folder.uri.fsPath, best).split(path.sep).join('/');
            await linkService.setLink(link);
          }
        }
      } catch {
        /* entry останется пустым — выберется вручную при первой отправке версии */
      }
      await setContexts();
      await attachAutoUpload();
      await refreshLatestCheck();
      void vscode.window.showInformationMessage(
        `Проект связан с отчетом «${picked.report.name ?? picked.report.id}»`,
      );
    } catch (e) {
      void vscode.window.showErrorMessage(
        `Не удалось получить список отчетов: ${e instanceof Error ? e.message : String(e)}`,
      );
    }
  });

  const unlinkCommand = vscode.commands.registerCommand('reportchecker.unlink', async () => {
    await linkService.clearLink();
    await setContexts();
    autoUpload?.detach();
    threads?.sync([]);
    decorations?.update([]);
    tree?.refresh();
  });

  const refreshCommand = vscode.commands.registerCommand('reportchecker.refreshIssues', async () => {
    await refreshLatestCheck();
  });

  const showLogCommand = vscode.commands.registerCommand('reportchecker.showLog', () => {
    showLog();
  });
  context.subscriptions.push(showLogCommand);

  const openInWebCommand = vscode.commands.registerCommand('reportchecker.openInWeb', async () => {
    const link = await linkService.getLink();
    if (!link) return;
    await checkService!.openInWeb(link.reportId);
  });

  const openIssueCommand = vscode.commands.registerCommand(
    'reportchecker.openIssue',
    async (fileIssue: unknown) => {
      if (!fileIssue || typeof fileIssue !== 'object' || !('issue' in (fileIssue as object))) return;
      await threads!.openIssue(fileIssue as Parameters<CommentThreads['openIssue']>[0]);
    },
  );

  const toggleAutoUploadCommand = vscode.commands.registerCommand('reportchecker.toggleAutoUpload', async () => {
    if (!autoUpload) return;
    const link = await linkService.getLink();
    if (!link) return;
    const target = !autoUpload.isPaused();
    autoUpload.setPaused(target);
    await context.workspaceState.update('reportchecker.autoUploadPaused', target);
    statusBar?.setAutoUpload(settings.autoUpload && !target);
    void vscode.window.showInformationMessage(
      target ? 'Автоотправка версий выключена' : 'Автоотправка версий включена',
    );
  });

  // --- команды тредов ---

  const replyCommand = vscode.commands.registerCommand(
    'reportchecker.replyToThread',
    async (commentReply: unknown) => {
      const { thread, text } = CommentThreads.extractReply(commentReply);
      if (!thread || !text) return;
      const issue = threads!.issueForThread(thread);
      if (!issue) return;
      await commentService!.reply(issue, cleanCommentText(text));
      thread.comments = threads!.controller.options ? thread.comments : thread.comments;
      threads!.updateIssue(issue);
    },
  );

  const markReadCommand = vscode.commands.registerCommand(
    'reportchecker.markThreadRead',
    async (...args: unknown[]) => {
      const issue = issueFromArgs(args);
      if (!issue) return;
      await issueService!.markIssueRead(issue);
      threads!.updateIssue(issue);
    },
  );

  const statusCommand = (id: string, status: 'Fixed' | 'Closed' | 'Open') =>
    vscode.commands.registerCommand(id, async (...args: unknown[]) => {
      const issue = issueFromArgs(args);
      if (!issue) return;
      await commentService!.setStatus(issue, status);
      threads!.updateIssue(issue);
      await reloadIssues();
    });

  const patchStatusCommand = (id: string, status: 'Accepted' | 'Rejected') =>
    vscode.commands.registerCommand(id, async (...args: unknown[]) => {
      const { issue, comment } = issueAndCommentFromArgs(args);
      if (!issue || !comment) return;
      await commentService!.setPatchStatus(issue, comment, status);
      threads!.updateIssue(issue);
    });

  const applyPatchCommand = vscode.commands.registerCommand(
    'reportchecker.applyPatch',
    async (...args: unknown[]) => {
      const { issue, comment } = issueAndCommentFromArgs(args);
      if (!issue || !comment?.patch) return;
      try {
        const applied = await patchService!.apply(issue.chapter ?? '', comment.patch.lines ?? []);
        if (applied) {
          await commentService!.setPatchStatus(issue, comment, 'Applied');
          void vscode.window.showInformationMessage('Исправление применено к файлу');
          await reloadIssues();
        } else {
          await commentService!.setPatchStatus(issue, comment, 'Failed');
        }
      } catch (e) {
        await commentService!.setPatchStatus(issue, comment, 'Failed');
        void vscode.window.showErrorMessage(
          `Не удалось применить исправление: ${e instanceof Error ? e.message : String(e)}`,
        );
      }
    },
  );

  const editCommentCommand = vscode.commands.registerCommand(
    'reportchecker.editComment',
    async (...args: unknown[]) => {
      const { issue, comment } = issueAndCommentFromArgs(args);
      if (!issue || !comment) return;
      const current = comment.content ?? '';
      const text = await vscode.window.showInputBox({
        title: 'Изменить комментарий',
        value: current,
      });
      if (text === undefined || text === current) return;
      await commentService!.editComment(issue, comment, text);
      threads!.updateIssue(issue);
    },
  );

  const deleteCommentCommand = vscode.commands.registerCommand(
    'reportchecker.deleteComment',
    async (...args: unknown[]) => {
      const { issue, comment } = issueAndCommentFromArgs(args);
      if (!issue || !comment) return;
      const choice = await vscode.window.showWarningMessage('Удалить комментарий?', 'Удалить');
      if (choice !== 'Удалить') return;
      await commentService!.deleteComment(issue, comment);
      threads!.updateIssue(issue);
    },
  );

  function issueFromArgs(args: unknown[]): Issue | undefined {
    for (const arg of args) {
      if (arg && typeof arg === 'object' && 'rcIssue' in (arg as object)) {
        return (arg as { rcIssue?: Issue }).rcIssue;
      }
      if (arg && typeof arg === 'object' && 'issue' in (arg as object)) {
        return (arg as { issue: Issue }).issue;
      }
    }
    return undefined;
  }

  function issueAndCommentFromArgs(args: unknown[]): { issue?: Issue; comment?: Comment } {
    for (const arg of args) {
      if (arg && typeof arg === 'object' && 'rcIssue' in (arg as object)) {
        const c = arg as { rcIssue?: Issue; rcComment?: Comment };
        return { issue: c.rcIssue, comment: c.rcComment };
      }
    }
    return {};
  }

  function cleanCommentText(text: string): string {
    return text.trim();
  }

  function getProviderForWorkspace() {
    // Сейчас поддерживается только Latex; перебираем зарегистрированные провайдеры.
    const providers = [new LatexFormatProvider()];
    return providers.find((p) => p.isEntryCandidate('.tex'));
  }

  function forceNewEntry(): boolean {
    // при первой отправке всегда даем выбрать файл
    return true;
  }

  // --- инициализация сервисов ---

  statusBar = new StatusBar();
  const onPhase = (phase: CheckPhase): void => {
    statusBar!.setPhase(phase);
    tree?.setCheckPhase(phase);
  };
  checkService = new CheckService(api, linkService, () => settings, onPhase);
  issueService = new IssueService(api, linkService);
  commentService = new CommentService(api, linkService, (issue) => threads?.updateIssue(issue));
  patchService = new PatchService(linkService);
  threads = new CommentThreads(commentService, issueService, patchService);
  decorations = new IssueDecorations();
  tree = new IssuesTreeDataProvider(issueService, context);
  autoUpload = new AutoUploadService(checkService, linkService, () => settings, getProviderForWorkspace);
  authService = new AuthService(context, authApi, () => settings, httpClient, async () => {
    await setContexts();
    statusBar?.setLoggedIn(authService!.isLoggedIn());
    tree?.refresh();
  });

  context.subscriptions.push(
    checkService,
    commentService,
    threads,
    decorations,
    autoUpload,
    authService,
    statusBar,
    loginCommand, logoutCommand, sendForCheckCommand, sendVersionCommand,
    linkReportCommand, unlinkCommand, refreshCommand, openInWebCommand,
    openIssueCommand, toggleAutoUploadCommand,
    replyCommand, markReadCommand,
    statusCommand('reportchecker.issueFixed', 'Fixed'),
    statusCommand('reportchecker.issueClosed', 'Closed'),
    statusCommand('reportchecker.issueReopen', 'Open'),
    patchStatusCommand('reportchecker.acceptPatch', 'Accepted'),
    patchStatusCommand('reportchecker.rejectPatch', 'Rejected'),
    applyPatchCommand, editCommentCommand, deleteCommentCommand,
    onDidChangeSettings(() => {
      const prevAutoUpload = settings.autoUpload;
      settings = readSettings();
      if (prevAutoUpload !== settings.autoUpload) void attachAutoUpload();
    }),
  );

  vscode.window.createTreeView('reportchecker.issues', {
    treeDataProvider: tree,
    showCollapseAll: true,
  });

  const init = async (): Promise<void> => {
    await setContexts();
    statusBar.setLoggedIn(authService!.isLoggedIn());
    if (authService.isLoggedIn()) {
      const info = await authService.getSessionInfo();
      await threads.setUserName(info.userName);
      await refreshLatestCheck();
      await attachAutoUpload();
      await reloadIssues();
    }
    tree.refresh();
  };

  void init();
}

export function deactivate(): void {
  /* сервисы освобождаются через context.subscriptions */
}
