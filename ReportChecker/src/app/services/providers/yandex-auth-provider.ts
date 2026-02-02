import {inject, Injectable} from '@angular/core';
import {Observable, of, switchMap} from 'rxjs';
import {IAuthProvider} from './auth-provider';
import {patchState, signalState} from '@ngrx/signals';
import {HttpClient} from '@angular/common/http';
import {toObservable} from '@angular/core/rxjs-interop';

interface YandexCredentials {
  access_token: string;
  refresh_token: string;
  expires_in: number;
}

interface YandexStore {
  credentials: YandexCredentials | null;
}

@Injectable({
  providedIn: 'root',
})
export class YandexAuthProvider implements IAuthProvider {
  private readonly httpClient: HttpClient = inject(HttpClient);

  key: string = "yandex";
  displayName: string = "Яндекс";
  url: string = `https://oauth.yandex.ru/authorize?response_type=code&client_id=130fbdc3303845ae953fb7d05c857d9a&redirect_uri=${window.location.protocol}//${window.location.host}/auth/yandex`;

  private readonly credentials$$ = signalState<YandexStore>({
    credentials: null,
  });
  readonly credentials$: Observable<YandexCredentials | null> = toObservable(this.credentials$$.credentials);

  load(): Observable<boolean> {
    const json = localStorage.getItem("yandexCredentials");
    if (!json)
      return of(false);
    const credentials = JSON.parse(json) as YandexCredentials;
    if (!credentials?.access_token)
      return of(false);
    patchState(this.credentials$$, {credentials});
    return of(true);
  }

  authorize(credentials: object): Observable<boolean> {
    patchState(this.credentials$$, {
      credentials: credentials as YandexCredentials,
    })
    localStorage.setItem("yandexCredentials", JSON.stringify(credentials));
    return of(true);
  }

  getIdToken(): Observable<string | null> {
    const credentials = this.credentials$$.credentials()
    if (credentials)
      return this.httpClient.get("https://login.yandex.ru/info?format=jwt", {
        headers: {
          Authorization: `OAuth ${credentials.access_token}`
        },
        responseType: "text",
      })
    return of(null);
  }
}
