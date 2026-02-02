import {inject, Injectable} from '@angular/core';
import {IAuthProvider} from './providers/auth-provider';
import {AccessTokenRequestSchema, ApiClient} from './api-client';
import {combineLatest, map, NEVER, Observable, of, skip, skipWhile, switchMap, take, tap} from 'rxjs';
import {YandexAuthProvider} from './providers/yandex-auth-provider';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';

interface AuthState {
  providerLoaded: number;
  isAuthorized: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<AuthState>({
    providerLoaded: 0,
    isAuthorized: false,
  });

  readonly providers$: IAuthProvider[] = [
    inject(YandexAuthProvider),
  ];

  readonly isAuthorized$ = toObservable(this.store$$).pipe(
    skipWhile(state => state.providerLoaded < this.providers$.length),
    map(state => state.isAuthorized),
  );

  authorize(provider: IAuthProvider, parameters: { [key: string]: string; }): Observable<boolean> {
    return this.apiClient.authPOST(provider.key, AccessTokenRequestSchema.fromJS({
      redirectUrl: provider.url,
      parameters,
    })).pipe(
      switchMap(response => provider.authorize(response)),
      switchMap(success => {
        if (!success)
          return of(false);
        return this.loadIdTokenFromProvider(provider);
      })
    );
  }

  private loadIdTokenFromProvider(provider: IAuthProvider): Observable<boolean> {
    return provider.getIdToken().pipe(
      map(idToken => {
        if (!idToken)
          return false;
        this.apiClient.setAuthorization(`Bearer ${idToken}`);
        return true;
      })
    );
  }

  loadProviders(): Observable<never> {
    return combineLatest(this.providers$.map(provider => provider.load().pipe(
      switchMap(authorized => {
        if (!authorized) {
          patchState(this.store$$, {
            providerLoaded: this.store$$.providerLoaded() + 1,
          });
          return NEVER;
        }
        return provider.getIdToken().pipe(
          tap(idToken => {
              this.apiClient.setAuthorization(`Bearer ${idToken}`);
            patchState(this.store$$, {
              isAuthorized: this.store$$.isAuthorized() || idToken !== null,
              providerLoaded: this.store$$.providerLoaded() + 1,
            });
          }),
        )
      }),
    ))).pipe(
      switchMap(() => NEVER),
    );
  }
}
