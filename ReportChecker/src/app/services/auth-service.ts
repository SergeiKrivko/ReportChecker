import {inject, Injectable} from '@angular/core';
import {ApiClient, UserCredentials} from './api-client';
import {catchError, combineLatest, map, NEVER, Observable, of, skipWhile, switchMap, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {Moment} from 'moment';

interface AuthState {
  isLoaded: boolean;
  isAuthorized: boolean;
  credentials: Credentials | null;
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
  });

  readonly isAuthorized$ = toObservable(this.store$$).pipe(
    skipWhile(state => !state.isLoaded),
    map(state => state.isAuthorized),
  );

  loadAuthorization(): Observable<boolean> {
    const credentialsJson = localStorage.getItem('reportCheckerCredentials');
    if (credentialsJson === null) {
      patchState(this.store$$, {isLoaded: true, isAuthorized: false, credentials: null});
      return of(false);
    }
    const credentials: Credentials = JSON.parse(credentialsJson);
    patchState(this.store$$, {isLoaded: true, isAuthorized: true, credentials});
    this.apiClient.setAuthorization("Bearer " + credentials.accessToken);
    return of(true);
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
      }),
      map(() => true),
      catchError(() => {
        patchState(this.store$$, {
          isLoaded: true,
          isAuthorized: false,
          credentials: null,
        });
        return of(false)
      }),
    );
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
