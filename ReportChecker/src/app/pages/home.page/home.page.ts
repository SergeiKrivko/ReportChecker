import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {NewReportDialog} from '../../components/new-report-dialog/new-report-dialog';
import {TuiButton, tuiDialog, TuiScrollbar, TuiSurface} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {AsyncPipe} from '@angular/common';
import {TuiCardLarge, TuiHeader} from '@taiga-ui/layout';
import {RouterLink} from '@angular/router';
import {Header} from '../../components/header/header';
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
    Header,
    TuiScrollbar,
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

  private readonly newReportDialog = tuiDialog(NewReportDialog, {
    dismissible: false,
    label: 'Новый отчет',
  });

  readonly limits$ = this.subscriptionService.limits$;
}
