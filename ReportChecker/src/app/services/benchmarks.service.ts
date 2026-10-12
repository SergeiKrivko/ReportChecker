import {inject, Injectable, signal} from '@angular/core';
import {
  ApiClient,
  BenchmarkCase,
  BenchmarkCaseDisplayMode,
  BenchmarkResult,
  BenchmarkRun,
  BenchmarkSummary,
  CreateBenchmarkRunSchema,
  LlmModel,
  LlmReasoningEffort
} from './api-client';
import {catchError, combineLatest, forkJoin, map, Observable, of, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {
  BenchmarkCaseEntity,
  BenchmarkModelRowEntity,
  BenchmarkResultEntity,
  BenchmarkRunEntity,
  BenchmarkSummaryEntity,
  BenchmarkTotalsEntity
} from '../entities/benchmark-entity';
import {LlmModelEntity} from '../entities/llm-model-entity';
import {Moment} from 'moment';
import {modelReasoningKey, reasoningEffortIndex} from '../helpers/llm-reasoning-effort';

/** Сколько последних прогонов запрашивать для списка. */
const RUNS_LIMIT = 200;

/**
 * Столбец сортировки таблицы: `model` — колонка модели, `cost` — средняя стоимость теста,
 * `total` — «Итого», любое другое значение — идентификатор теста (сортировка по его баллу).
 */
export type BenchmarkSortColumn = 'model' | 'cost' | 'total' | string;

export type BenchmarkSortDirection = 'asc' | 'desc';

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

  /** Текущий столбец сортировки таблицы. По умолчанию — «Итого». */
  readonly sortColumn$$ = signal<BenchmarkSortColumn>('total');

  /** Текущее направление сортировки. По умолчанию — по убыванию. */
  readonly sortDirection$$ = signal<BenchmarkSortDirection>('desc');

  /**
   * Матрица «модели × тесты».
   * Скрытые тесты (`displayMode: Hidden`) в таблицу не попадают и в «Итого» не учитываются.
   * Балл в «Итого» — среднее арифметическое баллов по тестам, а не доля суммарных
   * `matched`/`expected`: у тестов разное число известных ошибок и разное число прогонов,
   * поэтому отношение сумм давало бы крупным тестам больший вес.
   */
  readonly rows$: Observable<BenchmarkModelRowEntity[]> = combineLatest([
    this.summaries$,
    this.visibleCaseIds$,
    toObservable(this.sortColumn$$),
    toObservable(this.sortDirection$$),
  ]).pipe(
    map(([summaries, visibleCaseIds, sortColumn, sortDirection]) =>
      sortRows(buildRows(summaries, visibleCaseIds), sortColumn, sortDirection)),
  );

  /**
   * Переключает сортировку по столбцу: первый клик сортирует по убыванию, повторный —
   * по возрастанию, клик по другому столбцу начинает с убывания (кроме «Модели» и
   * названий тестов, где естественнее возрастание).
   */
  toggleSort(column: BenchmarkSortColumn) {
    if (this.sortColumn$$() === column) {
      this.sortDirection$$.update(direction => direction === 'desc' ? 'asc' : 'desc');
      return;
    }
    this.sortColumn$$.set(column);
    this.sortDirection$$.set(sortDirectionDefault(column));
  }

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
   * Гарантирует, что справочники тестов и моделей загружены. Нужен страницам,
   * которые открываются напрямую, минуя общую таблицу.
   */
  ensureLoaded() {
    if (this.store$$.loaded() && this.store$$.allCases().length > 0)
      return of(undefined);
    return this.load();
  }

  /**
   * Прогоны с фильтром по тройке (тест, модель, уровень рассуждений), сначала новые.
   * Без аргументов возвращает последние прогоны по всем тестам, моделям и уровням.
   * Мягко удалённые попадают в выборку только при `includeDeleted`.
   */
  runsFor(caseId?: string, modelId?: string,
          reasoningEffort?: LlmReasoningEffort | null,
          includeDeleted = false): Observable<BenchmarkRunEntity[]> {
    return this.apiClient.runsAllGET(caseId, modelId, undefined, reasoningEffort ?? undefined,
      RUNS_LIMIT, 0, includeDeleted).pipe(
      map(runs => runs.map(runToEntity)),
    );
  }

  /**
   * Один прогон со всеми строками результатов; `null`, если прогон не найден.
   * Удалённый прогон отдаётся только при `includeDeleted` и приходит с заполненным `deletedAt`.
   */
  runById(runId: string, includeDeleted = false): Observable<BenchmarkRunEntity | null> {
    if (!runId)
      return of(null);
    return this.apiClient.runsGET(runId, includeDeleted).pipe(
      map(run => runToEntity(run)),
      catchError(() => of(null)),
    );
  }

  /**
   * Мягко удаляет прогон: активный сначала отменяется на сервере, затем запись
   * помечается удалённой и пропадает из списка прогонов и сводки.
   */
  deleteRun(runId: string): Observable<void> {
    return this.apiClient.runsDELETE(runId);
  }

  /**
   * Запускает прогоны (по одному на каждую пару тест–модель) для каждого выбранного
   * уровня рассуждений и возвращает их идентификаторы.
   * Пустой список тестов означает «все доступные тесты».
   * Уровень рассуждений задаётся на весь запрос, поэтому на каждый уровень идёт свой запрос.
   */
  createRuns(caseIds: string[], modelIds: string[], useLlmMatching?: boolean,
             reasoningEfforts?: LlmReasoningEffort[]): Observable<string[]> {
    const efforts = reasoningEfforts?.length ? reasoningEfforts : [undefined];
    return forkJoin(efforts.map(reasoningEffort =>
      this.apiClient.runsAllPOST(CreateBenchmarkRunSchema.fromJS({
        caseIds,
        modelIds,
        useLlmMatching,
        reasoningEffort,
      })))).pipe(
      map(idLists => idLists.flat()),
    );
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

/** Главный показатель прогона — доля найденных известных ошибок. */
const matchedShare = (matched?: number, expected?: number): number =>
  expected ? (matched ?? 0) / expected : 0;

const runToEntity = (run: BenchmarkRun): BenchmarkRunEntity => ({
  id: run.id,
  caseId: run.caseId ?? '',
  caseName: run.caseName ?? run.caseId ?? '',
  modelId: run.modelId,
  modelName: run.modelDisplayName ?? run.modelId,
  reasoningEffort: run.reasoningEffort,
  status: run.status,
  createdAt: run.createdAt,
  startedAt: run.startedAt,
  finishedAt: run.finishedAt,
  deletedAt: run.deletedAt,
  durationMs: run.durationMs,
  failureReason: run.failureReason,

  expectedCount: run.expectedCount ?? 0,
  foundCount: run.foundCount ?? 0,
  matchedCount: run.matchedCount ?? 0,
  titleMatchCount: run.titleMatchCount ?? 0,
  priorityMatchCount: run.priorityMatchCount ?? 0,
  fixCheckedCount: run.fixCheckedCount ?? 0,
  fixMatchCount: run.fixMatchCount ?? 0,

  inputTokens: run.inputTokens ?? 0,
  outputTokens: run.outputTokens ?? 0,
  totalTokens: run.totalTokens ?? 0,
  totalRequests: run.totalRequests ?? 0,
  totalCost: run.totalCost ?? 0,

  matchedShare: matchedShare(run.matchedCount, run.expectedCount),
  fixMatchShare: matchedShare(run.fixMatchCount, run.fixCheckedCount),

  results: (run.results ?? []).map(resultToEntity),
});

const resultToEntity = (result: BenchmarkResult): BenchmarkResultEntity => ({
  id: result.id,

  expectedNumber: result.expectedNumber,
  errorClass: result.errorClass,
  chapter: result.chapter,
  line: result.line,

  expectedTitle: result.expectedTitle,
  expectedComment: result.expectedComment,
  expectedPriority: result.expectedPriority,

  foundIndex: result.foundIndex,
  foundTitle: result.foundTitle,
  foundComment: result.foundComment,
  foundPriority: result.foundPriority,

  isFound: result.isFound ?? false,
  titleMatch: result.titleMatch,
  priorityMatch: result.priorityMatch,
  priorityDelta: result.priorityDelta,

  matchingMethod: result.matchingMethod,
  matchScore: result.matchScore,
  matchingReason: result.matchingReason,

  fixMatchStatus: result.fixMatchStatus,
  expectedFix: result.expectedFix,
  foundFix: result.foundFix,
});

const summaryToEntity = (summary: BenchmarkSummary): BenchmarkSummaryEntity => ({
  caseId: summary.caseId ?? '',
  modelId: summary.modelId,
  reasoningEffort: summary.reasoningEffort,
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
  const rows = new Map<string, {row: BenchmarkModelRowEntity; accumulator: BenchmarkScoreAccumulator}>();

  for (const summary of summaries) {
    if (!visibleCaseIds.has(summary.caseId))
      continue;

    // Сводка группируется по тройке «тест + модель + уровень рассуждений»,
    // поэтому строка таблицы — это модель вместе с уровнем рассуждений.
    const key = modelReasoningKey(summary.modelId, summary.reasoningEffort);
    let entry = rows.get(key);
    if (!entry) {
      entry = {
        row: {
          key,
          modelId: summary.modelId,
          modelName: summary.modelDisplayName ?? summary.modelId,
          reasoningEffort: summary.reasoningEffort,
          totals: emptyTotals(),
          cells: {},
        },
        accumulator: emptyAccumulator(),
      };
      rows.set(key, entry);
    }
    entry.row.cells[summary.caseId] = summary;
    entry.accumulator = addToTotals(entry.accumulator, summary);
  }

  return [...rows.values()]
    .map(({row, accumulator}) => ({...row, totals: finalizeTotals(accumulator)}))
    // Строка без ни одного успешного прогона ничего не показывает: балл и стоимость
    // у неё пустые, поэтому в таблице она не нужна.
    .filter(row => row.totals.completedRunCount > 0);
};

/**
 * Направление по умолчанию для столбца: текстовые столбцы удобнее читать сверху вниз,
 * а числовые показатели — от лучшего к худшему.
 */
const sortDirectionDefault = (column: BenchmarkSortColumn): BenchmarkSortDirection =>
  column === 'model' ? 'asc' : 'desc';

/**
 * Значение столбца для строки. `undefined` означает «значения нет» — такие строки
 * всегда уезжают в конец, независимо от направления сортировки.
 */
const sortValue = (row: BenchmarkModelRowEntity,
                   column: BenchmarkSortColumn): number | string | undefined => {
  switch (column) {
    case 'model':
      return row.modelName;
    case 'cost':
      return row.totals.avgTestCost;
    case 'total':
      return row.totals.matchedShare;
    default:
      // Любой другой идентификатор — тест: сортируем по его баллу.
      return row.cells[column]?.matchedShare;
  }
};

/**
 * Сравнение строк по выбранному столбцу. Строки без значения столбца всегда оказываются
 * в конце списка. При равенстве значений порядок стабилизируется прежним «рейтинговым»
 * правилом: сначала лучший балл, затем больше оценённых тестов, затем имя модели.
 */
const compareRows = (a: BenchmarkModelRowEntity, b: BenchmarkModelRowEntity,
                     column: BenchmarkSortColumn,
                     direction: BenchmarkSortDirection): number => {
  const left = sortValue(a, column);
  const right = sortValue(b, column);

  const leftMissing = left == null;
  const rightMissing = right == null;
  if (leftMissing || rightMissing) {
    if (leftMissing && rightMissing)
      return tieBreak(a, b);
    return leftMissing ? 1 : -1;
  }

  const sign = direction === 'asc' ? 1 : -1;
  const compared = typeof left === 'string' || typeof right === 'string'
    ? String(left).localeCompare(String(right), 'ru')
    : left - right;
  return compared !== 0 ? sign * compared : tieBreak(a, b);
};

/** Устойчивый порядок строк с равными значениями: рейтинг, затем имя, затем уровень. */
const tieBreak = (a: BenchmarkModelRowEntity, b: BenchmarkModelRowEntity): number =>
  b.totals.matchedShare - a.totals.matchedShare
  || b.totals.scoredCaseCount - a.totals.scoredCaseCount
  || a.modelName.localeCompare(b.modelName, 'ru')
  || reasoningEffortIndex(a.reasoningEffort) - reasoningEffortIndex(b.reasoningEffort);

const sortRows = (rows: BenchmarkModelRowEntity[], column: BenchmarkSortColumn,
                  direction: BenchmarkSortDirection): BenchmarkModelRowEntity[] =>
  [...rows].sort((a, b) => compareRows(a, b, column, direction));

/**
 * Суммы, накапливаемые по тестам строки, плюс накопленные показатели по тестам.
 * Показатели копятся отдельно от сумм, потому что «Итого» — среднее по тестам,
 * а `totalMatched`/`totalExpected`/`totalExtra` в подсказке остаются суммами.
 */
interface BenchmarkScoreAccumulator {
  totals: BenchmarkTotalsEntity;
  matchedShareSum: number;
  scoredCaseCount: number;
  fixMatchShareSum: number;
  fixScoredCaseCount: number;
  testCostSum: number;
  costScoredCaseCount: number;
}

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
  completedRunCount: 0,
  lastRunAt: undefined,
  matchedShare: 0,
  scoredCaseCount: 0,
  fixMatchShare: 0,
  fixScoredCaseCount: 0,
  avgTestCost: undefined,
  costScoredCaseCount: 0,
  avgDurationMs: undefined,
});

const emptyAccumulator = (): BenchmarkScoreAccumulator => ({
  totals: emptyTotals(),
  matchedShareSum: 0,
  scoredCaseCount: 0,
  fixMatchShareSum: 0,
  fixScoredCaseCount: 0,
  testCostSum: 0,
  costScoredCaseCount: 0,
});

const addToTotals = (accumulator: BenchmarkScoreAccumulator,
                     summary: BenchmarkSummaryEntity): BenchmarkScoreAccumulator => ({
  totals: {
    ...accumulator.totals,
    totalExpected: accumulator.totals.totalExpected + summary.totalExpected,
    totalFound: accumulator.totals.totalFound + summary.totalFound,
    totalMatched: accumulator.totals.totalMatched + summary.totalMatched,
    totalExtra: accumulator.totals.totalExtra + summary.totalExtra,
    totalFixChecked: accumulator.totals.totalFixChecked + summary.totalFixChecked,
    totalFixMatched: accumulator.totals.totalFixMatched + summary.totalFixMatched,
    totalTokens: accumulator.totals.totalTokens + summary.totalTokens,
    totalRequests: accumulator.totals.totalRequests + summary.totalRequests,
    totalCost: accumulator.totals.totalCost + summary.totalCost,
    runCount: accumulator.totals.runCount + summary.runCount,
    completedRunCount: accumulator.totals.completedRunCount + summary.completedRunCount,
    lastRunAt: latest(accumulator.totals.lastRunAt, summary.lastRunAt),
    avgDurationMs: sumDuration(accumulator.totals, summary),
  },

  // Балл теста без известных ошибок не определён — в среднее такой тест не входит.
  matchedShareSum: accumulator.matchedShareSum
    + (summary.totalExpected === 0 ? 0 : summary.matchedShare),
  scoredCaseCount: accumulator.scoredCaseCount + (summary.totalExpected === 0 ? 0 : 1),

  // Тест, где ни одно исправление не проверялось, тоже не участвует в среднем.
  fixMatchShareSum: accumulator.fixMatchShareSum
    + (summary.totalFixChecked === 0 ? 0 : summary.fixMatchShare),
  fixScoredCaseCount: accumulator.fixScoredCaseCount + (summary.totalFixChecked === 0 ? 0 : 1),

  // Средняя стоимость одного прогона по этому тесту; суммируются именно средние
  // по тестам, чтобы тест с большим числом прогонов не перевешивал остальные.
  testCostSum: accumulator.testCostSum + testAverageCost(summary),
  costScoredCaseCount: accumulator.costScoredCaseCount + (summary.runCount === 0 ? 0 : 1),
});

/** Средняя стоимость одного прогона теста; `0`, если прогонов нет. */
const testAverageCost = (summary: BenchmarkSummaryEntity): number =>
  summary.runCount === 0 ? 0 : summary.totalCost / summary.runCount;

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

const average = (sum: number, count: number): number => count === 0 ? 0 : sum / count;

const finalizeTotals = (accumulator: BenchmarkScoreAccumulator): BenchmarkTotalsEntity => ({
  ...accumulator.totals,
  matchedShare: average(accumulator.matchedShareSum, accumulator.scoredCaseCount),
  scoredCaseCount: accumulator.scoredCaseCount,
  fixMatchShare: average(accumulator.fixMatchShareSum, accumulator.fixScoredCaseCount),
  fixScoredCaseCount: accumulator.fixScoredCaseCount,
  avgTestCost: accumulator.costScoredCaseCount === 0
    ? undefined
    : accumulator.testCostSum / accumulator.costScoredCaseCount,
  costScoredCaseCount: accumulator.costScoredCaseCount,
});
