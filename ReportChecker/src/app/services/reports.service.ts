import {inject, Injectable} from '@angular/core';
import {
  ApiClient,
  CreateCheckSchema,
  CreateReportSchema,
  Report,
  SourceInfo,
  TestSourceRequestSchema, UpdateReportSchema
} from './api-client';
import {catchError, first, map, NEVER, Observable, of, switchMap, take, tap} from 'rxjs';
import {ReportEntity} from '../entities/report-entity';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {AuthService} from './auth-service';
import {SourceInfoEntity} from '../entities/source-info-entity';

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
  private readonly authService = inject(AuthService);

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
    return this.authService.refreshToken().pipe(
      switchMap(() => this.apiClient.reportsAll()),
      tap(reports => {
        patchState(this.store$$, {
          reports: reports.map(reportToEntity),
          selectedReport: null,
          loaded: true,
        });
      })
    );
  }

  createReport(name: string, source: string, sourceProvider: string, format: string): Observable<string> {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.apiClient.reportsPOST(CreateReportSchema.fromJS({
        name: name,
        source: source,
        sourceProvider: sourceProvider,
        format: format,
      })))
    );
  }

  createCheck(source: string): Observable<boolean> {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.selectedReport$),
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

  renameReport(newName: string): Observable<any> {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.selectedReport$),
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.reportsPUT(report.id, UpdateReportSchema.fromJS({
            name: newName,
          }));
        return NEVER;
      }),
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

  deleteSelectedReport() {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.selectedReport$),
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.reportsDELETE(report.id);
        return NEVER;
      }),
      switchMap(() => this.loadReports()),
    );
  }

  testSource(provider: string, source: string) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.apiClient.testSource(TestSourceRequestSchema.fromJS({provider, source}))),
      map(sourceInfoToEntity),
    )
  }
}

const reportToEntity = (report: Report): ReportEntity => ({
  id: report.id ?? "",
  name: report.name ?? "",
  sourceProvider: report.sourceProvider ?? "File",
  source: report.source ?? null,
  format: report.format ?? "Latex",
});

const sourceInfoToEntity = (info: SourceInfo): SourceInfoEntity => ({
  status: info.status,
  format: info.format,
});

