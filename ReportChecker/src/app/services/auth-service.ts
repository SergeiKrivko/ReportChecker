import {inject, Injectable} from '@angular/core';
import {AccountInfo, ApiClient, ApiException, RefreshTokenRequestSchema, UserCredentials, UserInfo} from './api-client';
import {catchError, first, map, NEVER, Observable, of, skipWhile, switchMap, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import moment, {Moment} from 'moment';
import {AccountInfoEntity, UserInfoEntity} from '../entities/user-info-entity';

interface AuthState {
  isLoaded: boolean;
  isAuthorized: boolean;
  credentials: Credentials | null;
  userInfo: UserInfoEntity | null;
  isRefreshing: boolean;
}

interface Credentials {
  accessToken: string;
  refreshToken: string;
  expiresAt: Moment;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<AuthState>({
    isLoaded: false,
    isAuthorized: false,
    credentials: null,
    userInfo: null,
    isRefreshing: false,
  });

  readonly isAuthorized$ = toObservable(this.store$$).pipe(
    skipWhile(state => !state.isLoaded),
    map(state => state.isAuthorized),
  );
  readonly userInfo$ = toObservable(this.store$$.userInfo);
  private readonly isRefreshing$ = toObservable(this.store$$.isRefreshing);

  loadAuthorization(): Observable<boolean> {
    const credentialsJson = localStorage.getItem('reportCheckerCredentials');
    if (credentialsJson === null) {
      patchState(this.store$$, {isLoaded: true, isAuthorized: false, credentials: null});
      return of(false);
    }
    const credentials: Credentials = JSON.parse(credentialsJson);
    patchState(this.store$$, {credentials})
    this.apiClient.setAuthorization("Bearer " + credentials.accessToken);
    return this.refreshToken().pipe(
      switchMap(() => this.getUserInfo()),
      tap(() => patchState(this.store$$, {isLoaded: true, isAuthorized: true, credentials})),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  refreshToken(): Observable<boolean> {
    const expiresAt = this.store$$.credentials()?.expiresAt;
    if (expiresAt === undefined || moment().diff(expiresAt) < 0)
      return of(false);
    if (this.store$$.isRefreshing())
      return this.isRefreshing$.pipe(
        skipWhile(e => !e),
        first(),
      );
    patchState(this.store$$, {isRefreshing: true});
    return this.apiClient.refresh(RefreshTokenRequestSchema.fromJS({
      refreshToken: this.store$$.credentials()?.refreshToken
    })).pipe(
      map(credentialsToEntity),
      tap(credentials => {
        patchState(this.store$$, {
          credentials: credentials,
          isRefreshing: false,
        });
        localStorage.setItem("reportCheckerCredentials", JSON.stringify(credentials));
        this.apiClient.setAuthorization("Bearer " + credentials?.accessToken);
      }),
      map(() => true),
      catchError((err: ApiException) => {
        if (err.status === 401) {
          localStorage.removeItem("reportCheckerCredentials");
          patchState(this.store$$, {
            isAuthorized: false,
            credentials: null,
            userInfo: null,
            isRefreshing: false,
          });
        }
        return of(false)
      }),
    )
  }

  getToken(code: string): Observable<boolean> {
    return this.apiClient.token(code).pipe(
      map(credentialsToEntity),
      tap(credentials => {
        patchState(this.store$$, {
          isLoaded: true,
          isAuthorized: true,
          credentials: credentials,
        });
        localStorage.setItem("reportCheckerCredentials", JSON.stringify(credentials));
        this.apiClient.setAuthorization("Bearer " + credentials?.accessToken);
      }),
      switchMap(() => this.getUserInfo()),
      map(() => true),
      catchError(() => {
        patchState(this.store$$, {
          isLoaded: true,
          isAuthorized: false,
          credentials: null,
          userInfo: null,
        });
        return of(false)
      }),
    );
  }

  linkAccount(code: string) {
    return this.apiClient.link(code).pipe(
      switchMap(() => this.getUserInfo()),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  private getUserInfo(): Observable<UserInfoEntity> {
    return this.apiClient.userinfo().pipe(
      map(userInfoToEntity),
      tap(userInfo => patchState(this.store$$, {userInfo})),
      catchError((err: ApiException) => {
        if (err.status == 401)
          patchState(this.store$$, {
            isAuthorized: false,
            credentials: null,
            userInfo: null,
          });
        else
          console.error(err);
        return NEVER;
      })
    )
  }
}

const credentialsToEntity = (credentials: UserCredentials): Credentials | null => {
  if (credentials.accessToken == undefined || credentials.refreshToken === undefined || credentials.expiresAt === undefined)
    return null;
  return {
    accessToken: credentials.accessToken,
    refreshToken: credentials.refreshToken,
    expiresAt: credentials.expiresAt,
  }
}

const userInfoToEntity = (userInfo: UserInfo): UserInfoEntity => {
  return {
    id: userInfo.id,
    accounts: userInfo.accounts?.map(accountInfoToEntity) ?? [],
  }
}

const accountInfoToEntity = (accountInfo: AccountInfo): AccountInfoEntity => {
  return {
    provider: accountInfo.provider ?? "",
    id: accountInfo.id ?? "",
    name: accountInfo.name,
    email: accountInfo.email,
    avatarUrl: accountInfo.avatarUrl,
  }
}
