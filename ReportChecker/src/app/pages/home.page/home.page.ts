import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {TuiButton, TuiSurface} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {AsyncPipe} from '@angular/common';
import {TuiCardLarge, TuiHeader} from '@taiga-ui/layout';
import {RouterLink} from '@angular/router';
import {SubscriptionLimit} from '../../components/subscription-limit/subscription-limit';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {LimitReachedPipe} from '../../pipes/limit-reached-pipe';

@Component({
  selector: 'app-home.page',
  imports: [
    TuiButton,
    AsyncPipe,
    TuiCardLarge,
    TuiHeader,
    TuiSurface,
    RouterLink,
    SubscriptionLimit,
    LimitReachedPipe
  ],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly reportService = inject(ReportsService);
  private readonly subscriptionService = inject(SubscriptionsService);

  protected readonly reports$ = this.reportService.reports$;

  readonly limits$ = this.subscriptionService.limits$;
}
