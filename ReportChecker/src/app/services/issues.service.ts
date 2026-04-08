import {inject, Injectable} from '@angular/core';
import {ApiClient, Comment, CreateCommentSchema, IPatch, IPatchLine, PatchStatus, Issue, MarkReadSchema} from './api-client';
import {IssueEntity} from '../entities/issue-entity';
import {CommentEntity} from '../entities/comment-entity';
import moment from 'moment';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {
  combineLatest,
  first,
  interval,
  map,
  NEVER,
  Observable,
  of,
  switchMap,
  take,
  takeWhile,
  tap,
  timer
} from 'rxjs';
import {ReportsService} from './reports.service';
import {PatchEntity, PatchLineEntity, PatchStatusEntity} from '../entities/patch-entity';

interface IssuesStore {
  issues: IssueEntity[];
  selectedIssue: IssueEntity | null;
  isProgress: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class IssuesService {
  private readonly apiClient = inject(ApiClient);
  private readonly reportsService = inject(ReportsService);

  private readonly store$$ = signalState<IssuesStore>({
    issues: [],
    selectedIssue: null,
    isProgress: false,
  });

  readonly issues$ = toObservable(this.store$$.issues);
  readonly selectedIssue$ = toObservable(this.store$$.selectedIssue);
  readonly isProgress$ = toObservable(this.store$$.isProgress);

  loadIssues(reportId: string) {
    return this.apiClient.issuesAll(reportId).pipe(
      tap(reports => {
        patchState(this.store$$, {
          issues: reports.map(issueToEntity),
          selectedIssue: null,
        })
      }),
    );
  }

  loadIssuesOnReportChanged$ = this.reportsService.selectedReport$.pipe(
    tap(() => patchState(this.store$$, {issues: [], selectedIssue: null})),
    switchMap(report => {
      if (!report) return NEVER;

      let loaded = false;
      let currentInterval = 16000; // базовый интервал

      return timer(0, currentInterval).pipe(
        switchMap(() => this.apiClient.latest(report.id)),
        tap(check => patchState(this.store$$, {isProgress: check.status === "InProgress"})),
        switchMap(check => {
          // Если InProgress - поллим issues каждые 2 секунды
          if (check.status === "InProgress") {
            return interval(2000).pipe(
              switchMap(() => this.loadIssues(report.id)),
              switchMap(() => this.apiClient.latest(report.id)),
              tap(nextCheck => patchState(this.store$$, {isProgress: nextCheck.status === "InProgress"})),
              takeWhile(nextCheck => nextCheck.status === "InProgress", true)
            );
          }
          // Иначе просто загружаем issues по базовому интервалу
          if (!loaded) {
            loaded = true;
            return this.loadIssues(report.id).pipe(
              map(a => a.map(issueToEntity))
            );
          }
          return NEVER;
        })
      );
    })
  );

  addIssueComment(issueId: string, content: string | null, status: string | null) {
    return this.reportsService.selectedReport$.pipe(
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.commentsPOST(report.id, issueId, CreateCommentSchema.fromJS({content, status})).pipe(
            switchMap(commentId => this.pollComment(report.id, issueId, commentId)),
          );
        return NEVER;
      }),
    );
  }

  private reloadIssue(reportId: string, issueId: string): Observable<IssueEntity> {
    return combineLatest([
      this.apiClient.issues(reportId, issueId),
      this.issues$
    ]).pipe(
      first(),
      map(([iss, issues]) => {
        const issue = issueToEntity(iss);
        const index = issues.findIndex(i => i.id == issueId);
        issues[index] = issue;
        patchState(this.store$$, {issues: issues});

        if (issue.id == this.store$$.selectedIssue()?.id) {
          patchState(this.store$$, {selectedIssue: issue})
        }

        return issue;
      }),
    );
  }

  private pollComment(reportId: string, issueId: string, commentId: string): Observable<boolean> {
    return this.reloadIssue(reportId, issueId).pipe(
      switchMap(() => interval(2000)),
      switchMap(() => this.apiClient.commentsGET(reportId, issueId, commentId)),
      switchMap(comment => {
        if (comment?.progressStatus == "InProgress")
          return NEVER;
        if (comment?.progressStatus == "Completed") {
          return this.reloadIssue(reportId, issueId).pipe(map(() => true));
        }
        return of(false);
      }),
      first(),
    );
  }

  selectIssue(issueId: string) {
    return this.issues$.pipe(
      tap(issues => {
        patchState(this.store$$, {
          selectedIssue: issues.find(e => e.id == issueId),
        });
      })
    );
  }

  deselectIssue() {
    patchState(this.store$$, {selectedIssue: null});
  }

  markRead() {
    return combineLatest([
      this.reportsService.selectedReport$,
      this.selectedIssue$,
    ]).pipe(
      switchMap(([report, issue]) => {
        if (!report || !issue || issue.unreadCount == 0)
          return NEVER;
        return this.apiClient.read(report.id, issue.id, MarkReadSchema.fromJS({isRead: true})).pipe(
          switchMap(() => this.reloadIssue(report.id, issue.id)),
        );
      }),
    );
  }
}

const issueToEntity = (issue: Issue): IssueEntity => ({
  id: issue.id ?? "",
  title: issue.title ?? "",
  status: issue.status ?? "Open",
  priority: issue.priority ?? 10,
  comments: issue.comments?.map(commentToEntity) ?? [],
  chapter: issue.chapter ?? null,
  unreadCount: issue.comments?.filter(e => e.isRead === false).length ?? 0,
});


const commentToEntity = (comment: Comment): CommentEntity => ({
  id: comment.id ?? "",
  userId: comment.userId ?? "",
  content: comment.content ?? null,
  status: comment.status ?? null,
  progressStatus: comment.progressStatus ?? null,
  isRead: comment.isRead ?? null,
  patch: comment.patch ? patchToEntity(comment.patch) : undefined,
  createdAt: comment.createdAt ?? moment(),
  updatedAt: comment.modifiedAt ?? null,
});

const patchToEntity = (dto: IPatch): PatchEntity => ({
  id: dto.id,
  commentId: dto.commentId,
  status: statusMap[dto.status],
  lines: dto.lines?.map(patchLineToEntity) ?? [],
});

const patchLineToEntity = (dto: IPatchLine): PatchLineEntity => ({
  number: dto.number,
  content: dto.content,
  type: dto.type ?? "Unknown",
});

const statusMap: Record<PatchStatus, PatchStatusEntity> = {
  [PatchStatus.Pending]: PatchStatusEntity.Pending,
  [PatchStatus.InProgress]: PatchStatusEntity.InProgress,
  [PatchStatus.Completed]: PatchStatusEntity.Completed,
  [PatchStatus.Accepted]: PatchStatusEntity.Accepted,
  [PatchStatus.Rejected]: PatchStatusEntity.Rejected,
  [PatchStatus.Applied]: PatchStatusEntity.Applied,
  [PatchStatus.Failed]: PatchStatusEntity.Failed,
}
