import {inject, Injectable} from '@angular/core';
import {CurrentSubscriptionEntity} from '../entities/current-subscription-entity';
import {
  ApiClient, ApiException,
  CreateUserSubscriptionSchema,
  PaymentRequestSchema,
  SubscriptionPlan,
  UserSubscriptionsSchema
} from './api-client';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {catchError, EMPTY, map, Observable, of, switchMap, tap, throwError} from 'rxjs';
import {AuthService} from '../auth/auth.service';
import {SubscriptionPlanEntity} from '../entities/subscription-plan-entity';

interface SubscriptionsStore {
  current: CurrentSubscriptionEntity | null;
  plans: SubscriptionPlanEntity[];
}

@Injectable({
  providedIn: 'root',
})
export class SubscriptionsService {
  private readonly apiClient = inject(ApiClient);
  private readonly authService = inject(AuthService);

  private readonly store$$ = signalState<SubscriptionsStore>({
    current: null,
    plans: [],
  });

  readonly current$: Observable<CurrentSubscriptionEntity | null> = toObservable(this.store$$.current);
  readonly plans$: Observable<SubscriptionPlanEntity[]> = toObservable(this.store$$.plans);

  readonly loadLimits$ = toObservable(this.authService.isAuthenticated).pipe(
    switchMap(authorized => {
      if (!authorized)
        return of(null);
      return this.apiClient.current();
    }),
    tap(limits => patchState(this.store$$, {current: currentSubscriptionToEntity(limits)}))
  );

  readonly loadPlans$ = this.apiClient.plansAll().pipe(
    map(plans => plans.map(planToEntity)),
    tap(plans => patchState(this.store$$, {plans})),
  );

  createSubscription(offerId: string) {
    return this.apiClient.subscriptionsPOST(CreateUserSubscriptionSchema.fromJS({
      offerId
    }));
  }

  createPayment(subscriptionId: string): Observable<string | undefined> {
    return this.apiClient.payment(subscriptionId, PaymentRequestSchema.fromJS({})).pipe(
      map(resp => resp.url),
    );
  }

  checkPayments() {
    return this.apiClient.checkPayments().pipe(
      catchError((error: ApiException) => {
        if (error.status == 404)
          return EMPTY;
        return throwError(() => error);
      })
    );
  }
}

const currentSubscriptionToEntity = (dto: UserSubscriptionsSchema | null): CurrentSubscriptionEntity | null => {
  if (!dto)
    return null;
  return {
    active: dto.active,
    future: dto.future ?? [],
    tokensLimit: dto.tokensLimit,
    reportsLimit: dto.reportsLimit,
  };
}

const planToEntity = (dto: SubscriptionPlan): SubscriptionPlanEntity => ({
  id: dto.id,
  name: dto.name,
  description: dto.description,
  tokensLimit: dto.tokensLimit ?? 0,
  reportsLimit: dto.reportsLimit ?? 0,
  offers: dto.offers?.map(offerDto => ({
    id: offerDto.id,
    planId: offerDto.planId,
    price: offerDto.price,
    months: offerDto.months,
  })) ?? [],
});
