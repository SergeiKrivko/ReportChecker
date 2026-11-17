import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, RouterLink, RouterOutlet} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {filter, map, switchMap, withLatestFrom} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiButton, TuiLoader} from '@taiga-ui/core';
import {SharedReportsService} from '../../services/shared-reports.service';
import {PathService} from '../../services/path.service';
import {ReportEntity} from '../../entities/report-entity';

@Component({
  selector: 'app-shared-report-root.page',
  imports: [
    AsyncPipe,
    RouterOutlet,
    RouterLink,
    TuiButton,
    TuiLoader,
  ],
  templateUrl: './shared-report-root.page.html',
  styleUrl: './shared-report-root.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SharedReportRootPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sharedReportsService = inject(SharedReportsService);
  private readonly pathService = inject(PathService);

  protected readonly loaded$ = this.sharedReportsService.loaded$;
  protected readonly notFound$ = this.sharedReportsService.notFound$;

  ngOnInit() {
    this.route.params.pipe(
      map(params => params['token']),
      switchMap(token => this.sharedReportsService.load(token)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.pathService.add(this.sharedReportsService.report$.pipe(
      filter((report): report is ReportEntity => report !== null),
      withLatestFrom(this.sharedReportsService.token$),
      map(([report, token]) => ({
        name: report.name,
        link: `/shared/reports/${token}`,
        icon: "@tui.book",
      })),
    ), 0);
  }

  ngOnDestroy() {
    this.pathService.clear(0);
  }
}
