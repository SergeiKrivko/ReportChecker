import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {
  ActivatedRoute,
  IsActiveMatchOptions,
  RouterOutlet
} from '@angular/router';
import {ReportsService} from '../../services/reports.service';
import {combineLatest, map, NEVER, switchMap, take} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {PathService} from '../../services/path.service';

@Component({
  selector: 'app-report-root.page',
  imports: [
    RouterOutlet,
  ],
  templateUrl: './report-root.page.html',
  styleUrl: './report-root.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportRootPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reportsService = inject(ReportsService);
  private readonly pathService = inject(PathService);

  ngOnInit() {
    combineLatest([
      this.route.params,
      this.reportsService.loaded$,
    ]).pipe(
      switchMap(([params, loaded]) => {
        const appId = params['id'];
        if (appId && loaded)
          return this.reportsService.selectReport(appId);
        return NEVER;
      }),
      take(1),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.pathService.add(this.reportsService.selectedReport$.pipe(
      map(report => {
        return {
          name: report?.name,
          link: '/reports/' + report?.id,
          icon: "@tui.book"
        }
      })
    ), 0);
  }

  ngOnDestroy() {
    this.pathService.clear(0);
  }

  protected readonly options: IsActiveMatchOptions = {
    matrixParams: 'ignored',
    queryParams: 'ignored',
    paths: 'subset',
    fragment: 'exact',
  };
}
