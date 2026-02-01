import {inject, Injectable} from '@angular/core';
import {IAuthProvider} from './providers/auth-provider';
import {AccessTokenRequestSchema, ApiClient} from './api-client';
import {combineLatest, map, NEVER, Observable, of, skip, switchMap, tap} from 'rxjs';
import {YandexAuthProvider} from './providers/yandex-auth-provider';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiClient = inject(ApiClient);

  readonly providers$: IAuthProvider[] = [
    inject(YandexAuthProvider),
  ];

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

  loadProviders(): Observable<boolean> {
    return combineLatest(this.providers$.map(provider => provider.load().pipe(
      switchMap(authorized => {
        if (!authorized)
          return NEVER;
        return provider.getIdToken().pipe(
          tap(idToken => {
            if (idToken)
              this.apiClient.setAuthorization(`Bearer ${idToken}`);
          }),
        )
      }),
    ))).pipe(
      skip(1),
      switchMap((tokens) => {
        if (tokens.filter(e => e !== null).length > 0)
          return this.apiClient.authGET().pipe(switchMap(() => of(true)));
        return of(false);
      }),
    );
  }
}
