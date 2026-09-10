import * as vscode from 'vscode';
import type { Check, Issue } from '../api/types';
import { CheckPhase } from '../services/checkService';
import { FileIssue } from '../formats/formatProvider';
import { IssueService } from '../services/issueService';
import { LinkService, WorkspaceLink } from '../services/linkService';

type Node = FileGroupNode | IssueNode | NoLinkNode;

interface FileGroupNode extends vscode.TreeItem {
  kind: 'group';
  children: IssueNode[];
}

interface IssueNode extends vscode.TreeItem {
  kind: 'issue';
  fileIssue: FileIssue;
}

interface NoLinkNode extends vscode.TreeItem {
  kind: 'nolink';
}

/** Дерево ошибок с welcome-состояниями (login/linked управляются контекстами). */
export class IssuesTreeDataProvider implements vscode.TreeDataProvider<Node> {
  private static readonly NO_POSITION = '(без привязки к строке)';

  private readonly emitter = new vscode.EventEmitter<Node | undefined | void>();
  readonly onDidChangeTreeData = this.emitter.event;

  private fileIssues: FileIssue[] = [];
  private checkPhase: CheckPhase = 'idle';
  private latestCheck?: Check;
  private context: vscode.ExtensionContext;

  constructor(
    private readonly issueService: IssueService,
    context: vscode.ExtensionContext,
  ) {
    this.context = context;
    this.issueService.onIssues(() => {
      this.fileIssues = this.issueService.getFileIssues();
      this.refresh();
    });
  }

  refresh(): void {
    this.emitter.fire();
  }

  setCheckPhase(phase: CheckPhase, check?: Check): void {
    this.checkPhase = phase;
    this.latestCheck = check;
    void vscode.commands.executeCommand('setContext', 'reportchecker.checkInProgress',
      phase === 'queued' || phase === 'inProgress');
    this.refresh();
  }

  getTreeItem(element: Node): vscode.TreeItem {
    return element;
  }

  async getChildren(element?: Node): Promise<Node[]> {
    if (element && 'children' in element) return element.children;
    if (element) return [];

    const link = await this.context.workspaceState.get<WorkspaceLink | undefined>('reportchecker.link');
    if (!link) return [];

    const groups = new Map<string, IssueNode[]>();
    const noPosition: IssueNode[] = [];

    for (const fi of this.fileIssues) {
      const node = this.toIssueNode(fi);
      if (!fi.position) {
        noPosition.push(node);
        continue;
      }
      const key = fi.position.path;
      let arr = groups.get(key);
      if (!arr) {
        arr = [];
        groups.set(key, arr);
      }
      arr.push(node);
    }

    const nodes: Node[] = [];
    if (this.checkPhase === 'uploading' || this.checkPhase === 'queued' || this.checkPhase === 'inProgress') {
      nodes.push(this.phaseNode());
    }
    for (const [file, children] of groups) {
      nodes.push(this.groupNode(file, children));
    }
    if (noPosition.length > 0) {
      nodes.push(this.groupNode(IssuesTreeDataProvider.NO_POSITION, noPosition, true));
    }
    return nodes;
  }

  private toIssueNode(fi: FileIssue): IssueNode {
    const issue: Issue = fi.issue;
    const unread = (issue.comments ?? []).some((c) => c.isRead === false);
    const icon = this.issueIcon(issue.status);
    const lineSuffix = fi.position ? `:${fi.position.line}` : '';
    const item: IssueNode = {
      kind: 'issue',
      label: issue.title ?? 'Ошибка',
      tooltip: this.issueTooltip(fi),
      description: unread ? '● новые комментарии' : undefined,
      iconPath: icon,
      command: {
        title: 'Открыть ошибку',
        command: 'reportchecker.openIssue',
        arguments: [fi],
      },
      contextValue: 'rc-issue',
      fileIssue: fi,
    };
    void lineSuffix;
    return item;
  }

  private groupNode(file: string, children: IssueNode[], virtual = false): FileGroupNode {
    const label = virtual ? file : this.shortPath(file);
    return {
      kind: 'group',
      label,
      tooltip: file,
      collapsibleState: vscode.TreeItemCollapsibleState.Expanded,
      description: `${children.length}`,
      iconPath: virtual ? vscode.ThemeIcon.File : vscode.ThemeIcon.Folder,
      children,
    } as unknown as FileGroupNode;
  }

  private phaseNode(): NoLinkNode {
    const label =
      this.checkPhase === 'uploading' ? '$(cloud-upload) Отправка версии…'
        : this.checkPhase === 'queued' ? '$(watch) Проверка в очереди…'
          : '$(sync~spin) Проверка выполняется…';
    return {
      kind: 'nolink',
      label,
      iconPath: undefined,
    } as unknown as NoLinkNode;
  }

  private issueIcon(status: Issue['status']): vscode.ThemeIcon {
    switch (status) {
      case 'Fixed':
        return new vscode.ThemeIcon('check-all', new vscode.ThemeColor('testing.iconPassed'));
      case 'Closed':
        return new vscode.ThemeIcon('circle-slash', new vscode.ThemeColor('testing.iconQueued'));
      case 'InProgress':
        return new vscode.ThemeIcon('tools', new vscode.ThemeColor('editorWarning.foreground'));
      default:
        return new vscode.ThemeIcon('error', new vscode.ThemeColor('editorError.foreground'));
    }
  }

  private issueTooltip(fi: FileIssue): vscode.MarkdownString {
    const md = new vscode.MarkdownString();
    md.isTrusted = true;
    md.appendMarkdown(`**${escape(fi.issue.title ?? 'Ошибка')}**\n\n`);
    if (fi.issue.chapter) {
      md.appendMarkdown(`Глава: \`${fi.issue.chapter}\`\n\n`);
    }
    const comments = fi.issue.comments ?? [];
    const unread = comments.filter((c) => c.isRead === false).length;
    md.appendMarkdown(`Комментариев: ${comments.length}${unread > 0 ? ` (непрочитанных: ${unread})` : ''}\n\n`);
    md.appendMarkdown(`[Открыть комментарии](command:reportchecker.openIssue?${encodeURIComponent(JSON.stringify([fi]))})`);
    return md;
  }

  private shortPath(file: string): string {
    const folder = vscode.workspace.workspaceFolders?.[0];
    if (folder && file.startsWith(folder.uri.fsPath)) {
      return file.slice(folder.uri.fsPath.length + 1).split(/[\\/]/).join('/');
    }
    return file;
  }
}

function escape(s: string): string {
  return s.replace(/[*_`[\]]/g, '\\$&');
}
