import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpErrorResponse, HttpHeaders} from '@angular/common/http';
import {catchError, Observable, throwError} from 'rxjs';
import {API_BASE_URL} from './api-client';
import {AuthService} from '../auth/auth.service';
import {TuiAlertService} from '@taiga-ui/core';
import {
  GoogleDriveConnectionEntity,
  GoogleDriveFolderEntity,
  GoogleDriveSyncResultEntity,
} from '../entities/google-drive-entities';

@Injectable({providedIn: 'root'})
export class GoogleDriveService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly alerts = inject(TuiAlertService);
  private readonly baseUrl = inject(API_BASE_URL, {optional: true}) ?? '';

  private get headers(): HttpHeaders {
    return new HttpHeaders({Authorization: `Bearer ${this.authService.accessToken()}`});
  }

  connection(): Observable<GoogleDriveConnectionEntity> {
    return this.http
      .get<GoogleDriveConnectionEntity>(`${this.baseUrl}/api/v1/google/connection`, {headers: this.headers})
      .pipe(catchError(error => this.handleError(error)));
  }

  resources(folderId: string, pageToken?: string): Observable<GoogleDriveFolderEntity> {
    return this.http
      .get<GoogleDriveFolderEntity>(`${this.baseUrl}/api/v1/google/resources`, {
        headers: this.headers,
        params: pageToken ? {folderId, pageToken} : {folderId},
      })
      .pipe(catchError(error => this.handleError(error)));
  }

  sync(reportId: string, force = false): Observable<GoogleDriveSyncResultEntity> {
    return this.http
      .post<GoogleDriveSyncResultEntity>(`${this.baseUrl}/api/v1/google/reports/${reportId}/sync`, null, {
        headers: this.headers,
        params: {force},
      })
      .pipe(catchError(error => this.handleError(error)));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    const message = error.error?.title ?? error.message ?? 'Не удалось обратиться к Google Диску';
    this.alerts.open(message, {label: 'Google Диск', appearance: 'negative'}).subscribe();
    return throwError(() => error);
  }
}
