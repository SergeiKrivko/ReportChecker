import {ChangeDetectorRef, Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {from, map, NEVER, switchMap, tap} from 'rxjs';
import {ReportsService} from '../../services/reports.service';
import {SourceInfoEntity} from '../../entities/source-info-entity';
import {TuiButton, TuiNotification} from '@taiga-ui/core';
import {Router} from '@angular/router';

@Component({
  selector: 'app-source-tester',
  imports: [
    TuiNotification,
    TuiButton
  ],
  templateUrl: './source-tester.html',
  styleUrl: './source-tester.scss',
})
export class SourceTester implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  name = input<string | null>();
  source = input.required<string | null>();
  provider = input.required<string>();

  protected readonly source$ = toObservable(this.source);

  protected sourceInfo: SourceInfoEntity | undefined;

  ngOnInit() {
    this.source$.pipe(
      switchMap(source => {
        if (!source)
          return NEVER;
        return this.reportsService.testSource(this.provider(), source)
      }),
      tap(sourceInfo => {
        this.sourceInfo = sourceInfo;
        this.changeDetectorRef.detectChanges();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  createReport() {
    const source = this.source();
    const format = this.sourceInfo?.format;
    if (!source || !format)
      return;
    this.reportsService.createReport(this.name() || "New report", source, this.provider(), format).pipe(
      switchMap(reportId => this.reportsService.loadReports().pipe(map(() => reportId))),
      tap(console.log),
      switchMap(id => from(this.router.navigate(['/reports/' + id]))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
