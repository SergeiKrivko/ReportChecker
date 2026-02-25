import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {
  ActivatedRoute,
  IsActiveMatchOptions,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import {ReportsService} from '../../services/reports.service';
import {combineLatest, NEVER, switchMap, take, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiScrollbar} from '@taiga-ui/core';
import {Header} from "../../components/header/header";
import {IssuesService} from '../../services/issues.service';
import {TuiLet} from '@taiga-ui/cdk';

@Component({
  selector: 'app-report-root.page',
  imports: [
    AsyncPipe,
    RouterOutlet,
    TuiButton,
    RouterLinkActive,
    RouterLink,
    Header,
    TuiLet,
    TuiScrollbar
  ],
  templateUrl: './report-root.page.html',
  styleUrl: './report-root.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportRootPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);

  protected readonly selectedReport$ = this.reportsService.selectedReport$;
  protected readonly selectedIssue$ = this.issuesService.selectedIssue$;

  ngOnInit() {
    this.route.paramMap.pipe(
      tap(console.log),
    ).subscribe();

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
  }

  protected readonly options: IsActiveMatchOptions = {
    matrixParams: 'ignored',
    queryParams: 'ignored',
    paths: 'subset',
    fragment: 'exact',
  };
}
