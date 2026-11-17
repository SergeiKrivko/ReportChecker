import {inject, Injectable} from '@angular/core';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {catchError, forkJoin, Observable, of, switchMap, tap} from 'rxjs';
import {Moment} from 'moment';
import {
  ApiClient,
  CreateReportShareSchema,
  ReportAccessLevel,
  ReportShare,
  ReportShareLink,
  UpdateReportShareSchema
} from './api-client';

export interface ReportShareEntity {
  userId: string;
  accessLevel: ReportAccessLevel;
  name: string | null;
  login: string | null;
  email: string | null;
  avatarUrl: string | null;
  createdAt?: Moment;
}

export interface ReportShareLinkEntity {
  token: string;
  createdAt?: Moment;
}

interface ReportShareStore {
  shares: ReportShareEntity[];
  link: ReportShareLinkEntity | null;
  loaded: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class ReportShareService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<ReportShareStore>({
    shares: [],
    link: null,
    loaded: false,
  });

  readonly shares$ = toObservable(this.store$$.shares);
  readonly link$ = toObservable(this.store$$.link);
  readonly loaded$ = toObservable(this.store$$.loaded);

  load(reportId: string) {
    patchState(this.store$$, {loaded: false, link: null});
    return forkJoin({
      shares: this.apiClient.shareUsersAll(reportId),
      // Отсутствие публичной ссылки — нормальное состояние, а не ошибка.
      link: this.apiClient.shareLinkGET(reportId).pipe(catchError(() => of(null))),
    }).pipe(
      tap(({shares, link}) => patchState(this.store$$, {
        shares: shares.map(shareToEntity),
        link: link ? shareLinkToEntity(link) : null,
        loaded: true,
      })),
    );
  }

  share(reportId: string, loginOrEmail: string, accessLevel: ReportAccessLevel) {
    return this.apiClient.shareUsersPOST(reportId, CreateReportShareSchema.fromJS({
      loginOrEmail,
      accessLevel,
    })).pipe(
      switchMap(() => this.load(reportId)),
    );
  }

  update(reportId: string, userId: string, accessLevel: ReportAccessLevel) {
    return this.apiClient.shareUsersPUT(reportId, userId, UpdateReportShareSchema.fromJS({
      accessLevel,
    })).pipe(
      switchMap(() => this.load(reportId)),
    );
  }

  revoke(reportId: string, userId: string) {
    return this.apiClient.shareUsersDELETE(reportId, userId).pipe(
      switchMap(() => this.load(reportId)),
    );
  }

  createLink(reportId: string) {
    return this.saveLink(this.apiClient.shareLinkPOST(reportId));
  }

  rotateLink(reportId: string) {
    return this.saveLink(this.apiClient.shareLinkRotate(reportId));
  }

  revokeLink(reportId: string) {
    return this.apiClient.shareLinkDELETE(reportId).pipe(
      tap(() => patchState(this.store$$, {link: null})),
    );
  }

  private saveLink(link$: Observable<ReportShareLink>): Observable<ReportShareLink> {
    return link$.pipe(
      tap(link => patchState(this.store$$, {link: shareLinkToEntity(link)})),
    );
  }
}

const shareToEntity = (share: ReportShare): ReportShareEntity => ({
  userId: share.userId,
  accessLevel: share.accessLevel,
  name: share.name ?? null,
  login: share.login ?? null,
  email: share.email ?? null,
  avatarUrl: share.avatarUrl ?? null,
  createdAt: share.createdAt,
});

const shareLinkToEntity = (link: ReportShareLink): ReportShareLinkEntity => ({
  token: link.token ?? "",
  createdAt: link.createdAt,
});
