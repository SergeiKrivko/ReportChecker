import {Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {
  ApiClient,
  CreateSubscriptionPlanSchema,
  SubscriptionPlan
} from '../../services/api-client';
import {PathService} from '../../services/path.service';
import {BehaviorSubject, Observable, of, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton} from '@taiga-ui/core';
import {AdminSubscriptionPlan} from '../../components/admin-subscription-plan/admin-subscription-plan';

@Component({
  selector: 'app-admin-subscriptions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    AdminSubscriptionPlan
  ],
  templateUrl: './admin-subscriptions.page.html',
  styleUrl: './admin-subscriptions.page.scss',
})
export class AdminSubscriptionsPage implements OnInit, OnDestroy {
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pathService = inject(PathService);

  private readonly store$$ = new BehaviorSubject<SubscriptionPlan[]>([]);
  protected readonly plans$: Observable<SubscriptionPlan[]> = this.store$$;

  ngOnInit() {
    this.loadPlans().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.pathService.add(of({name: "Подписки", link: "/admin/subscriptions"}), 1);
  }

  ngOnDestroy() {
    this.pathService.clear(1);
  }

  private loadPlans() {
    return this.apiClient.plansAll().pipe(
      tap((plans: SubscriptionPlan[]) => this.store$$.next(plans)),
    );
  }

  protected addPlan() {
    this.apiClient.plansPOST(CreateSubscriptionPlanSchema.fromJS({
      displayName: "New plan",
      tokensLimit: 0,
      reportsLimit: 1,
    })).pipe(
      switchMap(() => this.loadPlans()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
