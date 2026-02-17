import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {
  ActivatedRoute,
  IsActiveMatchOptions,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';
import {ReportsService} from '../../services/reports.service';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {combineLatest, from, NEVER, switchMap, take} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TUI_CONFIRM, TuiConfirmData, TuiSegmented} from '@taiga-ui/kit';
import {TuiButton} from '@taiga-ui/core';

@Component({
  selector: 'app-report-root.page',
  imports: [
    AsyncPipe,
    RouterOutlet,
    TuiSegmented,
    TuiButton,
    RouterLinkActive,
    RouterLink
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
  private readonly router = inject(Router);
  private readonly dialogs = inject(TuiResponsiveDialogService);

  protected readonly selectedReport$ = this.reportsService.selectedReport$;

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
  }

  protected readonly options: IsActiveMatchOptions = {
    matrixParams: 'ignored',
    queryParams: 'ignored',
    paths: 'subset',
    fragment: 'exact',
  };

  protected deleteReport() {
    const data: TuiConfirmData = {
      content: 'Вы уверены, что хотите удалить этот отчет?',
      yes: 'Удалить',
      no: 'Отмена',
    };

    this.dialogs
      .open<boolean>(TUI_CONFIRM, {
        size: 's',
        data,
      }).pipe(
      switchMap((response) => {
        if (response)
          return this.reportsService.deleteSelectedReport();
        return NEVER;
      }),
      switchMap(() => from(this.router.navigate(['/']))),
    ).subscribe();
  }
}
