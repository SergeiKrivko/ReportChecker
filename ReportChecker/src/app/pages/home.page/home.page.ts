import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {TuiButton, TuiLoader, TuiSurface} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {AsyncPipe} from '@angular/common';
import {TuiCardLarge, TuiHeader} from '@taiga-ui/layout';
import {RouterLink} from '@angular/router';
import {SubscriptionLimit} from '../../components/subscription-limit/subscription-limit';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {LimitReachedPipe} from '../../pipes/limit-reached-pipe';
import {TuiAvatar, TuiBadge} from '@taiga-ui/kit';
import {IconBySourcePipe} from '../../pipes/icon-by-source-pipe';
import {MapPriorityPipe} from '../../pipes/map-priority-pipe';
import {DateFromNowPipe} from '../../pipes/date-from-now-pipe';

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
    LimitReachedPipe,
    TuiAvatar,
    IconBySourcePipe,
    MapPriorityPipe,
    TuiBadge,
    DateFromNowPipe,
    TuiLoader
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
  protected readonly loaded$ = this.reportService.loaded$;

  readonly limits$ = this.subscriptionService.current$;
}
