import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {UserInfoEntity} from '../entities/user-info-entity';
import {map, Observable, of, switchMap, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {AuthService} from './auth.service';
import {toObservable} from '@angular/core/rxjs-interop';

interface AuthState {
  isLoaded: boolean;
  userInfo: UserInfoEntity | null;
}

/** Идентификатор администратора в auth.nachert.art. Синхронизирован с `Security:AdminIds` на бэкенде. */
const ADMIN_USER_ID = 'b13fa26b-0a30-4558-a2cd-da2d68022bab';

@Injectable({
  providedIn: 'root',
})
export class AuthClient {
  private readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly baseUrl = "https://auth.nachert.art/";

  private readonly accessToken$ = toObservable(this.authService.accessToken);

  private readonly store$$ = signalState<AuthState>({
    isLoaded: false,
    userInfo: null,
  });

  readonly userInfo$ = toObservable(this.store$$.userInfo);

  /** Есть ли у пользователя права администратора. Проверяются и на сервере — это только для UI. */
  readonly isAdmin$: Observable<boolean> = this.userInfo$.pipe(
    map(userInfo => userInfo?.id === ADMIN_USER_ID),
  );


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
