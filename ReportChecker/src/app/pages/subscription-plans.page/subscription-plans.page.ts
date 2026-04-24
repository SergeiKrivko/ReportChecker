import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {AsyncPipe} from '@angular/common';
import {TuiCardLarge, TuiList} from '@taiga-ui/layout';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiButton, TuiScrollbar} from '@taiga-ui/core';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-subscription-plans.page',
  imports: [
    AsyncPipe,
    TuiCardLarge,
    TuiScrollbar,
    TuiList,
    TuiButton,
    RouterLink
  ],
  templateUrl: './subscription-plans.page.html',
  styleUrl: './subscription-plans.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionPlansPage implements OnInit {
  private readonly subscriptionsService = inject(SubscriptionsService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly plans$ = this.subscriptionsService.plans$;
  protected readonly current$ = this.subscriptionsService.current$;

  ngOnInit() {
    this.subscriptionsService.loadPlans$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected index = 1;
}
