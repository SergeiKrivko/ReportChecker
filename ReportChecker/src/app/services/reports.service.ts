import {inject, Injectable} from '@angular/core';
import {
  ApiClient,
  CreateCheckSchema,
  CreateReportSchema,
  IFileCheckSource,
  IFileReportSource,
  IGitHubCheckSource,
  IGitHubReportSource,
  IYandexDiskCheckSource,
  IYandexDiskReportSource,
  ImageProcessingMode,
  LlmModel,
  LlmReasoningEffort,
  SourceInfo,
  TestSourceRequestSchema,
  UpdateReportSchema
} from './api-client';
import {catchError, EMPTY, first, map, NEVER, Observable, of, switchMap, take, tap} from 'rxjs';
import {ImageProcessingModeEntity, ReportEntity} from '../entities/report-entity';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {SourceInfoEntity} from '../entities/source-info-entity';
import {LlmModelEntity} from '../entities/llm-model-entity';
import {reportToEntity} from '../helpers/report-entity-mappers';

interface ReportsStore {
  reports: ReportEntity[];
  selectedReport: ReportEntity | null;
  loaded: boolean;
  models: LlmModelEntity[];
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
    models: [],
  });

  readonly reports$ = toObservable(this.store$$.reports);
  readonly selectedReport$ = toObservable(this.store$$.selectedReport);
  readonly loaded$ = toObservable(this.store$$.loaded);
  readonly models$ = toObservable(this.store$$.models);

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

  loadModels() {
    return this.apiClient.modelsAll(false).pipe(
      map(e => e
        .map(llmModelToEntity)
        .sort((a, b) => ((a.displayName ?? "") > (b.displayName ?? "") ? 1 : -1))
      ),
      tap(models => patchState(this.store$$, {models}))
    );
  }

  createReport(name: string,
               source: IGitHubReportSource | IFileReportSource | IYandexDiskReportSource,
               sourceProvider: string,
               format: string,
               llmModelId?: string,
               imageProcessingMode: ImageProcessingModeEntity = ImageProcessingModeEntity.Disable,
               reasoningLevel: LlmReasoningEffort = LlmReasoningEffort.Default): Observable<string> {
    return this.apiClient.reportsPOST(CreateReportSchema.fromJS({
      name: name,
      source: sourceProvider == 'GitHub'
        ? {gitHub: source}
        : sourceProvider == 'YandexDisk'
          ? {yandexDisk: source}
          : {file: source},
      sourceProvider: sourceProvider,
      format: format,
      llmModelId,
      imageProcessingMode: imageProcessingModeReverseMap[imageProcessingMode],
      reasoningLevel: reasoningLevel,
    }));
  }

  createCheck(source: IGitHubCheckSource | IFileCheckSource | IYandexDiskCheckSource, id?: string): Observable<boolean> {
    return this.selectedReport$.pipe(
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.checksPOST(report.id, CreateCheckSchema.fromJS({
            source: report.sourceProvider == 'GitHub'
              ? {gitHub: source, id}
              : report.sourceProvider == 'YandexDisk'
                ? {yandexDisk: source, id}
                : {file: source, id},
          }));
        return NEVER;
      })
    ).pipe(
      catchError(() => of(false)),
      map(() => true),
    );
  }

  updateReport(newName: string, llmModelId?: string,
               imageProcessingMode: ImageProcessingModeEntity = ImageProcessingModeEntity.Disable,
               reasoningLevel: LlmReasoningEffort = LlmReasoningEffort.Default): Observable<any> {
    return this.selectedReport$.pipe(
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.reportsPUT(report.id, UpdateReportSchema.fromJS({
            name: newName,
            llmModelId,
            imageProcessingMode: imageProcessingModeReverseMap[imageProcessingMode],
            reasoningLevel: reasoningLevel,
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
    return this.selectedReport$.pipe(
      take(1),
      switchMap(report => {
        if (report)
          return this.apiClient.reportsDELETE(report.id);
        return NEVER;
      }),
      switchMap(() => this.loadReports()),
    );
  }

  testSource(provider: string, source: IGitHubReportSource | IFileReportSource | IYandexDiskReportSource) {
    return this.apiClient.testSource(TestSourceRequestSchema.fromJS({
      provider,
      source: provider == 'GitHub'
        ? {gitHub: source}
        : provider == 'YandexDisk'
          ? {yandexDisk: source}
          : {file: source}
    })).pipe(
      map(sourceInfoToEntity),
    )
  }

  cancelCheck() {
    return this.selectedReport$.pipe(
      switchMap(report => {
        if (!report)
          return EMPTY;
        return this.apiClient.latest(report.id).pipe(
          switchMap(latest => {
            if (!latest)
              return EMPTY;
            return this.apiClient.checksDELETE(report.id, latest.id);
          }),
        );
      })
    )
  }

  restartCheck() {
    return this.selectedReport$.pipe(
      switchMap(report => {
        if (!report)
          return EMPTY;
        return this.apiClient.restart(report.id);
      }),
    )
  }
}

const sourceInfoToEntity = (info: SourceInfo): SourceInfoEntity => ({
  status: info.status,
  format: info.format,
});

const llmModelToEntity = (info: LlmModel): LlmModelEntity => ({
  id: info.id,
  displayName: info.displayName,
  inputCoefficient: info.inputCoefficient,
  outputCoefficient: info.outputCoefficient,
  defaultReasoningLevel: info.defaultReasoningLevel ?? LlmReasoningEffort.Default,
  isRecommended: info.isRecommended ?? false,
});

const imageProcessingModeReverseMap: Record<ImageProcessingModeEntity, ImageProcessingMode> = {
  [ImageProcessingModeEntity.Auto]: ImageProcessingMode.Auto,
  [ImageProcessingModeEntity.Disable]: ImageProcessingMode.Disable,
  [ImageProcessingModeEntity.LowDetail]: ImageProcessingMode.LowDetail,
  [ImageProcessingModeEntity.HighDetail]: ImageProcessingMode.HighDetail,
};
