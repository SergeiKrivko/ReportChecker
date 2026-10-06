import {ChangeDetectionStrategy, Component, computed, effect, inject, signal} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {TuiAlertService, TuiButton, TuiLoader, TuiTextfield} from '@taiga-ui/core';
import {BenchmarksService} from '../../services/benchmarks.service';
import {BenchmarkCaseDisplayMode} from '../../services/api-client';
import {BenchmarkCaseEntity} from '../../entities/benchmark-entity';
import {LlmModelEntity} from '../../entities/llm-model-entity';

/**
 * Панель запуска прогонов бенчмарка: выбор тестов и моделей.
 * Прогон создаётся по одному на каждую пару «тест × модель».
 * Видна только администратору; права всё равно проверяются на сервере (политика `Admin`).
 */
@Component({
  selector: 'app-benchmark-runner',
  imports: [
    ReactiveFormsModule,
    TuiButton,
    TuiLoader,
    TuiTextfield,
  ],
  templateUrl: './benchmark-runner.html',
  styleUrl: './benchmark-runner.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BenchmarkRunner {
  private readonly benchmarksService = inject(BenchmarksService);
  private readonly alerts = inject(TuiAlertService);

  protected readonly cases = toSignal(this.benchmarksService.allCases$, {
    initialValue: [] as BenchmarkCaseEntity[],
  });
  protected readonly models = toSignal(this.benchmarksService.models$, {
    initialValue: [] as LlmModelEntity[],
  });
  protected readonly loaded = toSignal(this.benchmarksService.loaded$, {
    initialValue: false,
  });

  /** Отмеченные тесты и модели. В запрос всегда уходит явный список идентификаторов. */
  protected readonly selectedCaseIds = signal<ReadonlySet<string>>(new Set());
  protected readonly selectedModelIds = signal<ReadonlySet<string>>(new Set());
  protected readonly starting = signal(false);
  protected readonly useLlmMatching = signal(true);
  protected readonly modelFilter = new FormControl<string>('', {nonNullable: true});

  private readonly casesDefaulted = signal(false);

  constructor() {
    // Обычный сценарий — прогнать весь набор тестов, поэтому отмечаем его по умолчанию.
    effect(() => {
      if (this.casesDefaulted() || this.selectedCaseIds().size > 0)
        return;
      const runnable = this.runnableCases();
      if (runnable.length === 0)
        return;
      this.casesDefaulted.set(true);
      this.selectedCaseIds.set(new Set(runnable.map(benchmarkCase => benchmarkCase.id)));
    });
  }

  /** Бэкенд отклоняет запрос с незагруженным тестом — такие не предлагаются к запуску. */
  protected readonly runnableCases = computed(() =>
    this.cases().filter(benchmarkCase => !benchmarkCase.validationError));

  /** Список моделей с учётом фильтра по названию. */
  protected readonly visibleModels = computed(() => {
    const query = this.modelFilter.value.trim().toLowerCase();
    const models = this.models();
    return query
      ? models.filter(model => (model.displayName ?? '').toLowerCase().includes(query))
      : models;
  });

  /** Число будущих прогонов: тесты × модели. */
  protected readonly plannedRuns = computed(() =>
    this.selectedCaseIds().size * this.selectedModelIds().size);

  protected readonly canStart = computed(() =>
    !this.starting()
    && this.selectedCaseIds().size > 0
    && this.selectedModelIds().size > 0);

  protected readonly isLoading = computed(() => !this.loaded());

  protected isCaseSelected(caseId: string): boolean {
    return this.selectedCaseIds().has(caseId);
  }

  protected isModelSelected(modelId: string): boolean {
    return this.selectedModelIds().has(modelId);
  }

  /** Скрытые тесты не попадают в таблицу, но их можно запускать — об этом стоит предупредить. */
  protected isHidden(benchmarkCase: BenchmarkCaseEntity): boolean {
    return benchmarkCase.displayMode === BenchmarkCaseDisplayMode.Hidden;
  }

  protected toggleCase(caseId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedCaseIds.update(ids => this.withId(ids, caseId, checked));
  }

  protected toggleModel(modelId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedModelIds.update(ids => this.withId(ids, modelId, checked));
  }

  protected toggleMatching(event: Event): void {
    this.useLlmMatching.set((event.target as HTMLInputElement).checked);
  }

  protected setAllCases(selected: boolean) {
    this.selectedCaseIds.set(selected
      ? new Set(this.runnableCases().map(benchmarkCase => benchmarkCase.id))
      : new Set());
  }

  protected setAllModels(selected: boolean) {
    this.selectedModelIds.set(selected
      ? new Set(this.models().map(model => model.id))
      : new Set());
  }

  protected start() {
    if (!this.canStart())
      return;

    this.starting.set(true);
    this.benchmarksService.createRuns(
      [...this.selectedCaseIds()],
      [...this.selectedModelIds()],
      this.useLlmMatching(),
    ).subscribe({
      next: runIds => {
        this.starting.set(false);
        this.alerts.open(
          `Создано прогонов: ${runIds.length}. Результаты появятся по мере выполнения.`,
          {label: 'Бенчмарк запущен', appearance: 'positive'},
        ).subscribe();
        this.benchmarksService.reloadSummaries().subscribe();
      },
      error: () => {
        this.starting.set(false);
      },
    });
  }

  private withId(ids: ReadonlySet<string>, id: string, selected: boolean): ReadonlySet<string> {
    const next = new Set(ids);
    if (selected)
      next.add(id);
    else
      next.delete(id);
    return next;
  }
}
