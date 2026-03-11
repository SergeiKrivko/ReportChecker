import {inject, Injectable} from '@angular/core';
import {LimitsEntity} from '../entities/limits-entity';
import {ApiClient, Limits} from './api-client';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {AuthService} from './auth-service';
import {NEVER, Observable, switchMap, tap} from 'rxjs';

interface SubscriptionsStore {
  limits: LimitsEntity | null;
}

@Injectable({
  providedIn: 'root',
})
export class SubscriptionsService {
  private readonly apiClient = inject(ApiClient);
  private readonly authService = inject(AuthService);

  private readonly store$$ = signalState<SubscriptionsStore>({
    limits: null,
  });

  readonly limits$: Observable<LimitsEntity | null> = toObservable(this.store$$.limits);

  readonly loadLimits$ = this.authService.isAuthorized$.pipe(
    switchMap(authorized => {
      if (!authorized)
        return NEVER;
      return this.authService.refreshToken();
    }),
    switchMap(() => this.apiClient.limits()),
    tap(limits => patchState(this.store$$, {limits: limitsToEntity(limits)}))
  );
}

const limitsToEntity = (limits: Limits): LimitsEntity => {
  return limits;
}
