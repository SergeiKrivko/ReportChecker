import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {catchError, map, Observable, of, shareReplay} from 'rxjs';
import {API_BASE_URL} from './api-client';
import {PublicUserEntity} from '../entities/public-user-entity';

interface PublicUserResponse {
  username?: string | null;
  avatarUrl?: string | null;
}

const UNKNOWN_USER: PublicUserEntity = {
  username: null,
  avatarUrl: null,
};

/**
 * Публичные профили пользователей (логин и аватар) для отчётов, которыми
 * с текущим пользователем поделились. Ответы кэшируются: один отчёт может
 * встречаться в списке несколько раз.
 */
@Injectable({
  providedIn: 'root',
})
export class UserProfileService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL).replace(/\/+$/, '');

  private readonly cache = new Map<string, Observable<PublicUserEntity>>();

  get(userId: string): Observable<PublicUserEntity> {
    if (!userId)
      return of(UNKNOWN_USER);

    let cached = this.cache.get(userId);
    if (!cached) {
      cached = this.http
        .get<PublicUserResponse>(`${this.apiBaseUrl}/api/v1/users/${encodeURIComponent(userId)}`)
        .pipe(
          map(user => ({
            username: user.username ?? null,
            avatarUrl: user.avatarUrl ?? null,
          })),
          catchError(() => of(UNKNOWN_USER)),
          shareReplay({bufferSize: 1, refCount: false}),
        );
      this.cache.set(userId, cached);
    }
    return cached;
  }
}
