import {inject, Injectable} from '@angular/core';
import {ApiClient, CreateCheckSchema, CreateReportSchema, Report} from './api-client';
import {catchError, map, NEVER, Observable, of, switchMap, take, tap} from 'rxjs';
import {ReportEntity} from '../entities/report-entity';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';

interface ReportsStore {
  reports: ReportEntity[];
  selectedReport: ReportEntity | null;
  loaded: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<ReportsStore>({
    reports: [],
    selectedReport: null,
    loaded: false,
  });

  readonly reports$ = toObservable(this.store$$.reports);
  readonly selectedReport$ = toObservable(this.store$$.selectedReport);
  readonly loaded$ = toObservable(this.store$$.loaded);

  loadReports() {
    patchState(this.store$$, {
      loaded: false,
    });
    return this.apiClient.reportsAll().pipe(
      tap(reports => {
        patchState(this.store$$, {
          reports: reports.map(reportToEntity),
          selectedReport: null,
          loaded: true,
        });
      })
    );
  }

  createReport(name: string, source: string, format: string): Observable<never> {
    return this.apiClient.reportsPOST(CreateReportSchema.fromJS({
      name: name,
      source: source,
      sourceProvider: "File",
      format: format,
    })).pipe(
      switchMap(() => NEVER),
    );
  }

  createCheck(source: string): Observable<boolean> {
    return this.selectedReport$.pipe(
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.checks(report.id, CreateCheckSchema.fromJS({
            source: source,
          }));
        return NEVER;
      })
    ).pipe(
      catchError(() => of(false)),
      map(() => true),
    );
  }

  reportById(id: string): Observable<ReportEntity | undefined> {
    return this.reports$.pipe(
      map(reports => reports.find(r => r.id == id)),
    );
  }

  selectReport(reportId: string) {
    return this.reportById(reportId).pipe(
      take(1),
      tap(report => patchState(this.store$$, {selectedReport: report})),
    );
  }
}

const reportToEntity = (report: Report): ReportEntity => ({
  id: report.id ?? "",
  name: report.name ?? "",
  sourceProvider: report.sourceProvider ?? "File",
  source: report.source ?? null,
  format: report.format ?? "Latex",
});

