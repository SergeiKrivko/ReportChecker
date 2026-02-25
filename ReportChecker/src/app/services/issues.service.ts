import {inject, Injectable} from '@angular/core';
import {ApiClient, Comment, CreateCommentSchema, Issue} from './api-client';
import {IssueEntity} from '../entities/issue-entity';
import {CommentEntity} from '../entities/comment-entity';
import moment from 'moment';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {combineLatest, first, interval, map, NEVER, Observable, of, switchMap, take, takeWhile, tap} from 'rxjs';
import {ReportsService} from './reports.service';
import {AuthService} from './auth-service';

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
  private readonly authService = inject(AuthService);
  private readonly reportsService = inject(ReportsService);

  private readonly store$$ = signalState<IssuesStore>({
    issues: [],
    selectedIssue: null,
    isProgress: false,
  });

  readonly issues$ = toObservable(this.store$$.issues);
  readonly selectedIssue$ = toObservable(this.store$$.selectedIssue);
  readonly isProgress$ = toObservable(this.store$$.isProgress);

  private loadIssues(reportId: string) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.apiClient.issuesAll(reportId)),
      tap(reports => {
        patchState(this.store$$, {
          issues: reports.map(issueToEntity),
          selectedIssue: null,
        })
      }),
    );
  }

  loadIssuesOnReportChanged$ = this.authService.refreshToken().pipe(
    switchMap(() => this.reportsService.selectedReport$),
    tap(() => patchState(this.store$$, {issues: [], selectedIssue: null})),
    switchMap(report => {
      if (report)
        return interval(2000).pipe(
          switchMap(() => this.loadIssues(report.id)),
          switchMap(() => this.apiClient.latest(report.id)),
          tap(check => patchState(this.store$$, {isProgress: check.status == "InProgress"})),
          takeWhile(check => check.status == "InProgress"),
        );
      return NEVER;
    }),
    switchMap(() => NEVER),
  );

  addIssueComment(issueId: string, content: string | null, status: string | null) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.reportsService.selectedReport$),
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
    return this.authService.refreshToken().pipe(
      switchMap(() => combineLatest([
        this.apiClient.issues(reportId, issueId),
        this.issues$
      ])),
      first(),
      map(([issue, issues]) => {
        issues = issues.filter(i => i.id !== issueId);
        issues.push(issueToEntity(issue));
        patchState(this.store$$, {issues});
        return issueToEntity(issue);
      }),
    );
  }

  private pollComment(reportId: string, issueId: string, commentId: string): Observable<boolean> {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.reloadIssue(reportId, issueId)),
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
}

const issueToEntity = (issue: Issue): IssueEntity => ({
  id: issue.id ?? "",
  title: issue.title ?? "",
  status: issue.status ?? "Open",
  priority: issue.priority ?? 10,
  comments: issue.comments?.map(commentToEntity) ?? [],
  chapter: issue.chapter ?? null,
});


const commentToEntity = (comment: Comment): CommentEntity => ({
  id: comment.id ?? "",
  userId: comment.userId ?? "",
  content: comment.content ?? null,
  status: comment.status ?? null,
  progressStatus: comment.progressStatus ?? null,
  createdAt: comment.createdAt ?? moment(),
  updatedAt: comment.modifiedAt ?? null,
});
