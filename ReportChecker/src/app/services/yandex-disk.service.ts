import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from '@angular/common/http';
import {catchError, map, Observable, throwError} from 'rxjs';
import {API_BASE_URL} from './api-client';
import {AuthService} from '../auth/auth.service';
import {TuiAlertService} from '@taiga-ui/core';
import {
  YandexDiskConnectionEntity,
  YandexDiskFolderEntity,
  YandexDiskSyncResultEntity,
} from '../entities/yandex-disk-entities';

@Injectable({providedIn: 'root'})
export class YandexDiskService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly alerts = inject(TuiAlertService);
  private readonly baseUrl = inject(API_BASE_URL, {optional: true}) ?? '';

  private get headers(): HttpHeaders {
    return new HttpHeaders({Authorization: `Bearer ${this.authService.accessToken()}`});
  }

  connection(): Observable<YandexDiskConnectionEntity> {
    return this.http
      .get<YandexDiskConnectionEntity>(`${this.baseUrl}/api/v1/yandex/connection`, {headers: this.headers})
      .pipe(catchError(error => this.handleError(error)));
  }

  oauthUrl(): Observable<string> {
    return this.http
      .get<{ url: string }>(`${this.baseUrl}/api/v1/yandex/oauth/url`, {headers: this.headers})
      .pipe(
        map(response => response.url),
        catchError(error => this.handleError(error)),
      );
  }

  disconnect(): Observable<void> {
    return this.http
      .delete<void>(`${this.baseUrl}/api/v1/yandex/connection`, {headers: this.headers})
      .pipe(catchError(error => this.handleError(error)));
  }

  resources(path: string, offset = 0, limit = 200): Observable<YandexDiskFolderEntity> {
    return this.http
      .get<YandexDiskFolderEntity>(`${this.baseUrl}/api/v1/yandex/resources`, {
        headers: this.headers,
        params: {path, offset, limit},
      })
      .pipe(catchError(error => this.handleError(error)));
  }

  sync(reportId: string, force = false): Observable<YandexDiskSyncResultEntity> {
    return this.http
      .post<YandexDiskSyncResultEntity>(`${this.baseUrl}/api/v1/yandex/reports/${reportId}/sync`, null, {
        headers: this.headers,
        params: {force},
      })
      .pipe(catchError(error => this.handleError(error)));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    const message = error.error?.title ?? error.message ?? 'Не удалось обратиться к Яндекс.Диску';
    this.alerts.open(message, {label: 'Яндекс.Диск', appearance: 'negative'}).subscribe();
    return throwError(() => error);
  }
}
