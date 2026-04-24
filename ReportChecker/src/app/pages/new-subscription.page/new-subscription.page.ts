import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {map, NEVER, Observable, switchMap, tap} from 'rxjs';
import {TuiButton, TuiLabel, TuiLoader, TuiNotification} from '@taiga-ui/core';
import {AsyncPipe} from '@angular/common';
import {AsDayPipe} from '../../pipes/as-day-pipe';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {SubscriptionOfferByIdPipe} from '../../pipes/subscription-offer-by-id-pipe';
import {SubscriptionPlanByIdPipe} from '../../pipes/subscription-plan-by-id-pipe';
import {TuiCopy} from '@taiga-ui/kit';

@Component({
  selector: 'app-new-subscription.page',
  imports: [
    RouterLink,
    TuiButton,
    AsyncPipe,
    TuiLabel,
    TuiNotification,
    AsDayPipe,
    SubscriptionOfferByIdPipe,
    SubscriptionPlanByIdPipe,
    TuiCopy,
    TuiLoader
  ],
  templateUrl: './new-subscription.page.html',
  styleUrl: './new-subscription.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewSubscriptionPage implements OnInit {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly subscriptionsService = inject(SubscriptionsService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly offerId$: Observable<string | undefined> = this.activatedRoute.queryParams.pipe(
    map(params => params['offer'])
  );
  protected readonly subscription$ = this.offerId$.pipe(
    switchMap(offerId => {
      if (!offerId)
        return NEVER;
      return this.subscriptionsService.createSubscription(offerId);
    }),
  );

  ngOnInit() {
    this.subscriptionsService.loadPlans$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
