import {inject, Injectable} from '@angular/core';
import {
  ApiClient,
  BenchmarkCase,
  BenchmarkCaseDisplayMode,
  BenchmarkSummary,
  CreateBenchmarkRunSchema,
  LlmModel
} from './api-client';
import {catchError, combineLatest, forkJoin, map, Observable, of, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {
  BenchmarkCaseEntity,
  BenchmarkModelRowEntity,
  BenchmarkSummaryEntity,
  BenchmarkTotalsEntity
} from '../entities/benchmark-entity';
import {LlmModelEntity} from '../entities/llm-model-entity';
import {Moment} from 'moment';

interface BenchmarksStore {
  /** Все корректно загруженные тесты, включая скрытые (для запуска прогонов). */
  allCases: BenchmarkCaseEntity[];
  models: LlmModelEntity[];
  summaries: BenchmarkSummaryEntity[];
  loaded: boolean;
  error: string | null;
}

/**
 * Сводка по бенчмаркам: тесты (колонки) и модели (строки) в виде матрицы.
 */
@Injectable({
  providedIn: 'root',
})
export class BenchmarksService {
  private readonly apiClient = inject(ApiClient);

  private readonly store$$ = signalState<BenchmarksStore>({
    allCases: [],
    models: [],
    summaries: [],
    loaded: false,
    error: null,
  });

  /** Все тесты, включая скрытые, отсортированные по имени. */
  readonly allCases$ = toObservable(this.store$$.allCases).pipe(
    map(cases => [...cases].sort((a, b) => a.name.localeCompare(b.name, 'ru'))),
  );

  /** Тесты, отображаемые в таблице: без скрытых (`displayMode: Hidden`). */
  readonly cases$ = this.allCases$.pipe(
    map(cases => cases.filter(benchmarkCase => benchmarkCase.displayMode !== BenchmarkCaseDisplayMode.Hidden)),
  );
  readonly models$ = toObservable(this.store$$.models);
  readonly summaries$ = toObservable(this.store$$.summaries);
  readonly loaded$ = toObservable(this.store$$.loaded);
  readonly error$ = toObservable(this.store$$.error);

  /** Идентификаторы отображаемых тестов (без скрытых). */
  private readonly visibleCaseIds$ = this.cases$.pipe(
    map(cases => new Set(cases.map(benchmarkCase => benchmarkCase.id))),
  );

  /**
   * Матрица «модели × тесты», отсортированная по главному показателю.
   * Скрытые тесты (`displayMode: Hidden`) в таблицу не попадают и в «Итого» не учитываются.
   */
  readonly rows$: Observable<BenchmarkModelRowEntity[]> = combineLatest([
    this.summaries$,
    this.visibleCaseIds$,
  ]).pipe(
    map(([summaries, visibleCaseIds]) => buildRows(summaries, visibleCaseIds)),
  );

  load() {
    patchState(this.store$$, {loaded: false, error: null});
    return forkJoin({
      cases: this.apiClient.cases(),
      models: this.apiClient.modelsAll(true),
      summaries: this.apiClient.summary(undefined, undefined),
    }).pipe(
      tap(({cases, models, summaries}) => {
        patchState(this.store$$, {
          allCases: cases.map(caseToEntity),
          models: models
            .map(modelToEntity)
            .sort((a, b) => (a.displayName ?? '').localeCompare(b.displayName ?? '', 'ru')),
          summaries: summaries.map(summaryToEntity),
          loaded: true,
        });
      }),
      catchError(() => {
        patchState(this.store$$, {
          allCases: [],
          models: [],
          summaries: [],
          loaded: true,
          error: 'Не удалось загрузить результаты бенчмарков',
        });
        return of(undefined);
      }),
    );
  }

  /**
   * Запускает прогоны (по одному на пару тест–модель) и возвращает их идентификаторы.
   * Пустой список тестов означает «все доступные тесты».
   */
  createRuns(caseIds: string[], modelIds: string[], useLlmMatching?: boolean) {
    return this.apiClient.runsAllPOST(CreateBenchmarkRunSchema.fromJS({
      caseIds,
      modelIds,
      useLlmMatching,
    }));
  }

  /** Перезагружает сводку, чтобы подхватить только что созданные прогоны. */
  reloadSummaries() {
    return this.apiClient.summary(undefined, undefined).pipe(
      tap(summaries => patchState(this.store$$, {summaries: summaries.map(summaryToEntity)})),
    );
  }
}

const caseToEntity = (benchmarkCase: BenchmarkCase): BenchmarkCaseEntity => ({
  id: benchmarkCase.id ?? '',
  name: benchmarkCase.name ?? benchmarkCase.id ?? '',
  description: benchmarkCase.description,
  format: benchmarkCase.format,
  displayMode: benchmarkCase.displayMode,
  expectedCount: benchmarkCase.expected?.length ?? 0,
  validationError: benchmarkCase.validationError,
});

const modelToEntity = (model: LlmModel): LlmModelEntity => ({
  id: model.id,
  displayName: model.displayName,
  inputCoefficient: model.inputCoefficient,
  outputCoefficient: model.outputCoefficient,
});

const summaryToEntity = (summary: BenchmarkSummary): BenchmarkSummaryEntity => ({
  caseId: summary.caseId ?? '',
  modelId: summary.modelId,
  caseName: summary.caseName,
  modelDisplayName: summary.modelDisplayName,

  runCount: summary.runCount ?? 0,
  completedRunCount: summary.completedRunCount ?? 0,
  failedRunCount: summary.failedRunCount ?? 0,
  cancelledRunCount: summary.cancelledRunCount ?? 0,

  avgDurationMs: summary.avgDurationMs,
  minDurationMs: summary.minDurationMs,
  maxDurationMs: summary.maxDurationMs,
  lastRunAt: summary.lastRunAt,

  totalExpected: summary.totalExpected ?? 0,
  totalFound: summary.totalFound ?? 0,
  totalMatched: summary.totalMatched ?? 0,
  totalExtra: summary.totalExtra ?? 0,
  totalTitleMatched: summary.totalTitleMatched ?? 0,
  totalPriorityMatched: summary.totalPriorityMatched ?? 0,
  totalFixChecked: summary.totalFixChecked ?? 0,
  totalFixMatched: summary.totalFixMatched ?? 0,

  totalInputTokens: summary.totalInputTokens ?? 0,
  totalOutputTokens: summary.totalOutputTokens ?? 0,
  totalTokens: summary.totalTokens ?? 0,
  totalRequests: summary.totalRequests ?? 0,
  totalCost: summary.totalCost ?? 0,

  matchedShare: summary.matchedShare ?? 0,
  titleMatchShare: summary.titleMatchShare ?? 0,
  priorityMatchShare: summary.priorityMatchShare ?? 0,
  fixMatchShare: summary.fixMatchShare ?? 0,
});

const buildRows = (summaries: BenchmarkSummaryEntity[],
                   visibleCaseIds: Set<string>): BenchmarkModelRowEntity[] => {
  const rows = new Map<string, BenchmarkModelRowEntity>();

  for (const summary of summaries) {
    if (!visibleCaseIds.has(summary.caseId))
      continue;

    let row = rows.get(summary.modelId);
    if (!row) {
      row = {
        modelId: summary.modelId,
        modelName: summary.modelDisplayName ?? summary.modelId,
        totals: emptyTotals(),
        cells: {},
      };
      rows.set(summary.modelId, row);
    }
    row.cells[summary.caseId] = summary;
    row.totals = addToTotals(row.totals, summary);
  }

  return [...rows.values()]
    .map(row => ({...row, totals: finalizeTotals(row.totals)}))
    .sort((a, b) => b.totals.matchedShare - a.totals.matchedShare
      || b.totals.totalMatched - a.totals.totalMatched
      || a.modelName.localeCompare(b.modelName, 'ru'));
};

const emptyTotals = (): BenchmarkTotalsEntity => ({
  totalExpected: 0,
  totalFound: 0,
  totalMatched: 0,
  totalExtra: 0,
  totalFixChecked: 0,
  totalFixMatched: 0,
  totalTokens: 0,
  totalRequests: 0,
  totalCost: 0,
  runCount: 0,
  lastRunAt: undefined,
  matchedShare: 0,
  fixMatchShare: 0,
  avgDurationMs: undefined,
});

const addToTotals = (totals: BenchmarkTotalsEntity,
                     summary: BenchmarkSummaryEntity): BenchmarkTotalsEntity => ({
  ...totals,
  totalExpected: totals.totalExpected + summary.totalExpected,
  totalFound: totals.totalFound + summary.totalFound,
  totalMatched: totals.totalMatched + summary.totalMatched,
  totalExtra: totals.totalExtra + summary.totalExtra,
  totalFixChecked: totals.totalFixChecked + summary.totalFixChecked,
  totalFixMatched: totals.totalFixMatched + summary.totalFixMatched,
  totalTokens: totals.totalTokens + summary.totalTokens,
  totalRequests: totals.totalRequests + summary.totalRequests,
  totalCost: totals.totalCost + summary.totalCost,
  runCount: totals.runCount + summary.runCount,
  lastRunAt: latest(totals.lastRunAt, summary.lastRunAt),
  avgDurationMs: sumDuration(totals, summary),
});

const sumDuration = (totals: BenchmarkTotalsEntity,
                     summary: BenchmarkSummaryEntity): number | undefined => {
  if (summary.avgDurationMs == null)
    return totals.avgDurationMs;
  const runs = totals.runCount;
  const total = (totals.avgDurationMs ?? 0) * runs + summary.avgDurationMs * summary.runCount;
  return Math.round(total / (runs + summary.runCount));
};

const latest = (a?: Moment, b?: Moment): Moment | undefined => {
  if (!a)
    return b;
  if (!b)
    return a;
  return b.isAfter(a) ? b : a;
};

const share = (part: number, total: number): number => total === 0 ? 0 : part / total;

const finalizeTotals = (totals: BenchmarkTotalsEntity): BenchmarkTotalsEntity => ({
  ...totals,
  matchedShare: share(totals.totalMatched, totals.totalExpected),
  fixMatchShare: share(totals.totalFixMatched, totals.totalFixChecked),
});
