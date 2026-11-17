import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {RouterLink} from '@angular/router';
import {map, Observable} from 'rxjs';
import {TuiAppearance, TuiButton, TuiIcon, TuiLoader, TuiNotification, TuiSurface} from '@taiga-ui/core';
import {TuiCard} from '@taiga-ui/layout';
import {TuiAvatar, TuiBadge, TuiSegmented} from '@taiga-ui/kit';
import {SharedReportsService} from '../../services/shared-reports.service';
import {IssueEntity} from '../../entities/issue-entity';
import {IssueHeader} from '../../components/issue-header/issue-header';
import {ReportOwner} from '../../components/report-owner/report-owner';
import {IconBySourcePipe} from '../../pipes/icon-by-source-pipe';
import {DateFromNowPipe} from '../../pipes/date-from-now-pipe';

@Component({
  selector: 'app-shared-report.page',
  imports: [
    AsyncPipe,
    RouterLink,
    TuiButton,
    TuiIcon,
    TuiAppearance,
    TuiLoader,
    TuiNotification,
    TuiSurface,
    TuiCard,
    TuiAvatar,
    TuiBadge,
    TuiSegmented,
    IssueHeader,
    ReportOwner,
    IconBySourcePipe,
    DateFromNowPipe,
  ],
  templateUrl: './shared-report.page.html',
  styleUrl: './shared-report.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SharedReportPage implements OnInit {
  private readonly sharedReportsService = inject(SharedReportsService);

  protected readonly report$ = this.sharedReportsService.report$;
  protected readonly check$ = this.sharedReportsService.check$;
  protected readonly canDownload$ = this.sharedReportsService.canDownload$;
  protected readonly issues$ = this.sharedReportsService.issues$;
  protected readonly selectedStatus$: Observable<number> = this.sharedReportsService.selectedStatus$.pipe(
    map(status => statusIndexMap[status ?? '']),
  );

  protected readonly activeIssues$ = this.issuesByStatus('Open');
  protected readonly closedIssues$ = this.issuesByStatus('Closed');
  protected readonly fixedIssues$ = this.issuesByStatus('Fixed');

  protected readonly isProgress$ = this.check$.pipe(
    map(check => check?.status === 'InProgress' || check?.status === 'Queued'),
  );
  protected readonly isFailed$ = this.check$.pipe(
    map(check => check?.status === 'Failed'),
  );
  protected readonly isCancelled$ = this.check$.pipe(
    map(check => check?.status === 'Cancelled'),
  );

  ngOnInit() {
    this.sharedReportsService.deselectIssue();
  }

  protected selectStatus(index: number) {
    this.sharedReportsService.selectStatus(indexStatusMap[index]);
  }

  protected refresh() {
    this.sharedReportsService.refresh().subscribe();
  }

  protected downloadSource() {
    this.sharedReportsService.downloadSource().subscribe(url => {
      if (url)
        window.location.href = url;
    });
  }

  private issuesByStatus(status: string): Observable<IssueEntity[]> {
    return this.sharedReportsService.allIssues$.pipe(
      map(issues => issues.filter(e => e.status === status)),
    );
  }
}

const statusIndexMap: Record<string, number> = {
  'Open': 0,
  'Closed': 1,
  'Fixed': 2,
  '': 3,
};
const indexStatusMap: Record<number, string | null> = {
  0: 'Open',
  1: 'Closed',
  2: 'Fixed',
  3: null,
};
