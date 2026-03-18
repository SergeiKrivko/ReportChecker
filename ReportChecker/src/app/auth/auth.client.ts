import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {UserInfoEntity} from '../entities/user-info-entity';
import {Observable, of, switchMap, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {AuthService} from './auth.service';
import {toObservable} from '@angular/core/rxjs-interop';

interface AuthState {
  isLoaded: boolean;
  userInfo: UserInfoEntity | null;
}

@Injectable({
  providedIn: 'root',
})
export class AuthClient {
  private readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly baseUrl = "https://auth.nachert.art/"

  private readonly accessToken$ = toObservable(this.authService.accessToken);

  private readonly store$$ = signalState<AuthState>({
    isLoaded: false,
    userInfo: null,
  });

  readonly userInfo$ = toObservable(this.store$$.userInfo);

  loadUserInfo() {
    return this.accessToken$.pipe(
      switchMap(token => {
        if (!token)
          return of(null);
        return this.http.get(this.baseUrl + 'api/v1/auth/userinfo', {
          headers: {
            Authorization: `Bearer ${token}`,
          }
        })
      }),
      tap(resp => patchState(this.store$$, {userInfo: resp as UserInfoEntity})),
    );
  }

  getLinkCode(): Observable<null | string> {
    const accessToken = this.authService.accessToken();
    if (!accessToken)
      return of(null);
    return this.http.get(this.baseUrl + 'api/v1/auth/linkCode', {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
      responseType: 'text',
    });
  }
}
