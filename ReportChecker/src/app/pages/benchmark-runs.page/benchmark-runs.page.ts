import {ChangeDetectionStrategy, Component, computed, inject, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {takeUntilDestroyed, toObservable, toSignal} from '@angular/core/rxjs-interop';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {TuiButton, TuiLabel, TuiLoader, TuiTextfield} from '@taiga-ui/core';
import {TuiBadge, TuiDataListWrapperComponent, TuiSelectDirective} from '@taiga-ui/kit';
import {combineLatest, distinctUntilChanged, filter, map, startWith, switchMap, tap} from 'rxjs';
import {BenchmarksService} from '../../services/benchmarks.service';
import {BenchmarkCaseEntity, BenchmarkRunEntity} from '../../entities/benchmark-entity';
import {LlmModelEntity} from '../../entities/llm-model-entity';
import {ProgressStatus} from '../../services/api-client';
import {Moment} from 'moment';

/**
 * Список прогонов бенчмарка с фильтрами по тесту и модели.
 * Выборка отражается в query-параметрах (`?case=…&model=…`), чтобы ссылкой можно было поделиться.
 */
@Component({
  selector: 'app-benchmark-runs.page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TuiBadge,
    TuiButton,
    TuiDataListWrapperComponent,
    TuiLabel,
    TuiLoader,
    TuiSelectDirective,
    TuiTextfield,
  ],
  templateUrl: './benchmark-runs.page.html',
  styleUrl: './benchmark-runs.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BenchmarkRunsPage {
  private readonly benchmarksService = inject(BenchmarksService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly cases = toSignal(this.benchmarksService.allCases$, {
    initialValue: [] as BenchmarkCaseEntity[],
  });
  protected readonly models = toSignal(this.benchmarksService.models$, {
    initialValue: [] as LlmModelEntity[],
  });
  protected readonly loaded = toSignal(this.benchmarksService.loaded$, {
    initialValue: false,
  });

  protected readonly caseControl = new FormControl<BenchmarkCaseEntity | null>(null);
  protected readonly modelControl = new FormControl<LlmModelEntity | null>(null);

  protected readonly runsLoading = signal(true);
  protected readonly runs = signal<BenchmarkRunEntity[]>([]);

  /** Выбранные фильтры в виде сигналов — от них зависят заголовок и запрос прогонов. */
  private readonly selectedCase = toSignal(
    this.caseControl.valueChanges.pipe(startWith(this.caseControl.value)),
    {initialValue: null},
  );
  private readonly selectedModel = toSignal(
    this.modelControl.valueChanges.pipe(startWith(this.modelControl.value)),
    {initialValue: null},
  );

  /** Заголовок страницы: без фильтров показываем последние прогоны по всем парам. */
  protected readonly heading = computed(() => {
    const caseName = this.selectedCase()?.name;
    const modelName = this.selectedModel()?.displayName;
    if (caseName && modelName)
      return `${caseName} · ${modelName}`;
    return caseName ?? modelName ?? 'Все прогоны';
  });

  /** Выбрана ли конкретная пара «тест + модель» — от этого зависит текст пустого состояния. */
  protected readonly hasSelection = computed(() =>
    !!this.selectedCase() || !!this.selectedModel());

  constructor() {
    this.benchmarksService.ensureLoaded().pipe(
      takeUntilDestroyed(),
    ).subscribe();

    // Прогоны подгружаются на каждое изменение фильтров, но только после того,
    // как загружены справочники: иначе фильтр по идентификатору ничего не найдёт.
    combineLatest([
      toObservable(this.selectedCase),
      toObservable(this.selectedModel),
      toObservable(this.loaded),
    ]).pipe(
      filter(([, , loaded]) => loaded),
      map(([benchmarkCase, model]) => ({caseId: benchmarkCase?.id, modelId: model?.id})),
      distinctUntilChanged((a, b) => a.caseId === b.caseId && a.modelId === b.modelId),
      tap(() => this.runsLoading.set(true)),
      switchMap(selection => this.benchmarksService.runsFor(selection.caseId, selection.modelId)),
      takeUntilDestroyed(),
    ).subscribe({
      next: runs => {
        this.runs.set(runs);
        this.runsLoading.set(false);
      },
      error: () => {
        this.runs.set([]);
        this.runsLoading.set(false);
      },
    });

    // Восстанавливаем фильтры из query-параметров только после загрузки справочников:
    // иначе глубокая ссылка `?case=…` не нашла бы тест и выборка потерялась.
    combineLatest([
      this.activatedRoute.queryParamMap,
      toObservable(this.loaded),
    ]).pipe(
      filter(([, loaded]) => loaded),
      takeUntilDestroyed(),
    ).subscribe(([params]) => {
      this.syncSelection(params.get('case'), params.get('model'));
    });

    this.caseControl.valueChanges.pipe(
      takeUntilDestroyed(),
    ).subscribe(() => this.applyQuery());

    this.modelControl.valueChanges.pipe(
      takeUntilDestroyed(),
    ).subscribe(() => this.applyQuery());
  }

  protected stringifyCase(benchmarkCase?: BenchmarkCaseEntity | null): string {
    return benchmarkCase?.name ?? 'Все тесты';
  }

  protected stringifyModel(model?: LlmModelEntity | null): string {
    return model?.displayName ?? 'Все модели';
  }

  /** Доля найденных известных ошибок в процентах — главный показатель прогона. */
  protected percent(value: number): number {
    return Math.round(value * 100);
  }

  protected level(run: BenchmarkRunEntity): string {
    if (this.isActive(run))
      return 'active';
    if (run.status === ProgressStatus.Failed)
      return 'failed';
    if (run.expectedCount === 0)
      return 'empty';
    const value = run.matchedShare;
    if (value >= 0.9)
      return 'excellent';
    if (value >= 0.75)
      return 'good';
    if (value >= 0.5)
      return 'fair';
    return 'poor';
  }

  protected isActive(run: BenchmarkRunEntity): boolean {
    return run.status === ProgressStatus.Queued
      || run.status === ProgressStatus.InProgress
      || run.status === ProgressStatus.CancellationRequested;
  }

  protected isFailed(run: BenchmarkRunEntity): boolean {
    return run.status === ProgressStatus.Failed;
  }

  /**
   * В `failureReason` прилетает полный стек исключения, поэтому в списке
   * показываем только первую строку — как правило, это и есть суть ошибки.
   */
  protected failureSummary(run: BenchmarkRunEntity): string {
    const reason = (run.failureReason ?? '').split('\n')[0].trim();
    if (!reason)
      return '';
    return reason.length > 160 ? `${reason.slice(0, 160)}…` : reason;
  }

  protected statusText(run: BenchmarkRunEntity): string {
    switch (run.status) {
      case ProgressStatus.Queued:
        return 'В очереди';
      case ProgressStatus.InProgress:
        return 'Выполняется';
      case ProgressStatus.Completed:
        return 'Завершён';
      case ProgressStatus.Failed:
        return 'Ошибка';
      case ProgressStatus.Cancelled:
        return 'Отменён';
      case ProgressStatus.CancellationRequested:
        return 'Отменяется';
      default:
        return '—';
    }
  }

  protected statusAppearance(run: BenchmarkRunEntity): string {
    if (this.isActive(run))
      return 'info';
    switch (run.status) {
      case ProgressStatus.Completed:
        return 'positive';
      case ProgressStatus.Failed:
        return 'negative';
      case ProgressStatus.Cancelled:
        return 'warning';
      default:
        return 'neutral';
    }
  }

  /** Дата и время в фиксированном формате, независимо от локали браузера. */
  protected date(value?: Moment): string {
    return value ? value.format('DD.MM.YYYY HH:mm') : '—';
  }

  protected duration(ms?: number): string {
    if (ms == null)
      return '—';
    if (ms < 1000)
      return `${ms} мс`;
    return `${(ms / 1000).toFixed(1)} с`;
  }

  protected tokens(value: number): string {
    if (value === 0)
      return '—';
    if (value < 1000)
      return `${value}`;
    return `${(value / 1000).toFixed(value < 10000 ? 1 : 0)} тыс.`;
  }

  protected cost(value: number): string {
    if (value === 0)
      return '—';
    return `${value.toFixed(2)} ₽`;
  }

  protected reset() {
    this.caseControl.setValue(null);
    this.modelControl.setValue(null);
  }

  /** Восстановление фильтров из query-параметров; без нужды значения не перезаписываются. */
  private syncSelection(caseId: string | null, modelId: string | null) {
    const benchmarkCase = this.cases().find(e => e.id === caseId) ?? null;
    const model = this.models().find(e => e.id === modelId) ?? null;

    if (this.caseControl.value?.id !== benchmarkCase?.id)
      this.caseControl.setValue(benchmarkCase);

    if (this.modelControl.value?.id !== model?.id)
      this.modelControl.setValue(model);
  }

  /** Отражает выбранные фильтры в адресной строке, не добавляя запись в историю. */
  private applyQuery() {
    if (!this.loaded())
      return;

    void this.router.navigate([], {
      relativeTo: this.activatedRoute,
      queryParams: {
        case: this.caseControl.value?.id ?? null,
        model: this.modelControl.value?.id ?? null,
      },
      replaceUrl: true,
    });
  }
}
