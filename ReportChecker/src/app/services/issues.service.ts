import {inject, Injectable} from '@angular/core';
import {ApiClient, Comment, CreateCommentSchema, Issue} from './api-client';
import {IssueEntity} from '../entities/issue-entity';
import {CommentEntity} from '../entities/comment-entity';
import moment from 'moment';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {combineLatest, map, NEVER, of, switchMap, take, tap} from 'rxjs';
import {ReportsService} from './reports.service';

interface IssuesStore {
  issues: IssueEntity[];
  selectedIssue: IssueEntity | null;
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
  });

  readonly issues$ = toObservable(this.store$$.issues);
  readonly selectedIssue$ = toObservable(this.store$$.selectedIssue);

  loadIssuesOnReportChanged$ = this.reportsService.selectedReport$.pipe(
    switchMap(report => {
      if (report)
        return this.apiClient.issuesAll(report.id);
      return NEVER;
    }),
    tap(reports => {
      patchState(this.store$$, {
        issues: reports.map(issueToEntity),
        selectedIssue: null,
      })
    }),
    switchMap(() => NEVER),
  );

  addIssueComment(issueId: string, content: string | null, status: string | null) {
    return this.reportsService.selectedReport$.pipe(
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.commentsPOST(report.id, issueId, CreateCommentSchema.fromJS({content, status})).pipe(
            map(() => true),
          );
        return of(false);
      }),
    );
  }

  reloadIssueComments(issueId: string) {
    return combineLatest([
      this.reportsService.selectedReport$.pipe(
        take(1),
        switchMap(report => {
          if (report)
            return this.apiClient.commentsAll(report.id, issueId);
          return NEVER;
        })
      ),
      this.issues$
    ]).pipe(
      tap(([comments, issues]) => {
        const issue = issues.find(e => e.id === issueId);
        if (issue) {
          issue.comments = comments.map(commentToEntity);
          patchState(this.store$$, {issues})
        }
      }),
      switchMap(() => of(true)),
    )
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
  createdAt: comment.createdAt ?? moment(),
  updatedAt: comment.modifiedAt ?? null,
});
