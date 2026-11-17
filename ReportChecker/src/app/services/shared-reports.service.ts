import {inject, Injectable} from '@angular/core';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {catchError, combineLatest, forkJoin, map, Observable, of, take, tap} from 'rxjs';
import {Moment} from 'moment';
import {ApiClient, Check, FileBucketDto} from './api-client';
import {ReportEntity} from '../entities/report-entity';
import {IssueEntity} from '../entities/issue-entity';
import {reportToEntity} from '../helpers/report-entity-mappers';
import {issueToEntity} from '../helpers/issue-entity-mappers';

export interface SharedCheckEntity {
  id: string;
  status: string;
  createdAt?: Moment;
}

interface SharedReportsStore {
  token: string | null;
  report: ReportEntity | null;
  issues: IssueEntity[];
  selectedIssue: IssueEntity | null;
  selectedStatus: string | null;
  check: SharedCheckEntity | null;
  canDownload: boolean;
  loaded: boolean;
  notFound: boolean;
}

/**
 * Данные отчёта, открытого по публичной ссылке. Использует анонимные
 * эндпоинты `/api/v1/shared/reports/{token}` и не требует авторизации.
 */
@Injectable({
  providedIn: 'root',
})
export class SharedReportsService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<SharedReportsStore>({
    token: null,
    report: null,
    issues: [],
    selectedIssue: null,
    selectedStatus: 'Open',
    check: null,
    canDownload: false,
    loaded: false,
    notFound: false,
  });

  readonly report$ = toObservable(this.store$$.report);
  readonly token$ = toObservable(this.store$$.token);
  readonly loaded$ = toObservable(this.store$$.loaded);
  readonly notFound$ = toObservable(this.store$$.notFound);
  readonly check$ = toObservable(this.store$$.check);
  readonly canDownload$ = toObservable(this.store$$.canDownload);
  readonly selectedIssue$ = toObservable(this.store$$.selectedIssue);
  readonly selectedStatus$ = toObservable(this.store$$.selectedStatus);
  readonly allIssues$ = toObservable(this.store$$.issues);
  readonly issues$ = combineLatest([this.allIssues$, this.selectedStatus$]).pipe(
    map(([issues, status]) => issues.filter(e => !status || e.status === status)),
  );

  load(token: string) {
    patchState(this.store$$, {
      token,
      loaded: false,
      notFound: false,
      report: null,
      issues: [],
      selectedIssue: null,
      check: null,
      canDownload: false,
    });

    return forkJoin({
      report: this.apiClient.sharedReportsGET(token),
      issues: this.apiClient.issuesAll2(token),
      checks: this.apiClient.checksAll2(token),
    }).pipe(
      tap(({report, issues, checks}) => patchState(this.store$$, {
        report: reportToEntity(report),
        issues: issues.map(issueToEntity),
        check: latestCheck(checks),
        canDownload: checks.some(e => e.status !== 'Failed' && e.status !== 'Cancelled'),
        loaded: true,
      })),
      map(() => undefined),
      catchError(() => {
        patchState(this.store$$, {loaded: true, notFound: true});
        return of(undefined);
      }),
    );
  }

  refresh() {
    const token = this.store$$.token();
    return token ? this.load(token) : of(undefined);
  }

  selectIssue(issueId: string): Observable<IssueEntity | null> {
    return this.allIssues$.pipe(
      take(1),
      map(issues => issues.find(e => e.id === issueId) ?? null),
      tap(issue => patchState(this.store$$, {selectedIssue: issue})),
    );
  }

  deselectIssue() {
    patchState(this.store$$, {selectedIssue: null});
  }

  selectStatus(status: string | null) {
    patchState(this.store$$, {selectedStatus: status});
  }

  downloadSource(): Observable<string | undefined> {
    const token = this.store$$.token();
    if (!token)
      return of(undefined);
    return this.apiClient.file(token, FileBucketDto.Default).pipe(
      map(response => response?.url),
    );
  }
}

const latestCheck = (checks: Check[]): SharedCheckEntity | null => {
  const latest = [...checks]
    .sort((a, b) => (b.createdAt?.valueOf() ?? 0) - (a.createdAt?.valueOf() ?? 0))[0];
  return latest ? checkToEntity(latest) : null;
};

const checkToEntity = (check: Check): SharedCheckEntity => ({
  id: check.id ?? "",
  status: check.status ?? "",
  createdAt: check.createdAt,
});
