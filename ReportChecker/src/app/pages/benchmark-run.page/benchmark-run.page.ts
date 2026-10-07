import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {takeUntilDestroyed, toSignal} from '@angular/core/rxjs-interop';
import {TuiButton, TuiLoader} from '@taiga-ui/core';
import {TuiBadge} from '@taiga-ui/kit';
import {map, switchMap} from 'rxjs';
import {BenchmarksService} from '../../services/benchmarks.service';
import {BenchmarkResultEntity, BenchmarkRunEntity} from '../../entities/benchmark-entity';
import {BenchmarkFixMatchStatus, BenchmarkMatchMethod, ProgressStatus} from '../../services/api-client';
import {Moment} from 'moment';

/**
 * Подробности одного прогона: сводка, все известные ошибки теста вместе с ответом модели
 * и отдельным списком «лишние» ошибки, которых в тесте не было.
 */
@Component({
  selector: 'app-benchmark-run.page',
  imports: [
    RouterLink,
    TuiBadge,
    TuiButton,
    TuiLoader,
  ],
  templateUrl: './benchmark-run.page.html',
  styleUrl: './benchmark-run.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BenchmarkRunPage {
  private readonly benchmarksService = inject(BenchmarksService);
  private readonly activatedRoute = inject(ActivatedRoute);

  private readonly runId$ = this.activatedRoute.paramMap.pipe(
    map(params => params.get('runId') ?? ''),
  );

  /** `undefined` — прогон ещё загружается, `null` — не найден. */
  protected readonly run = toSignal(
    this.runId$.pipe(switchMap(runId => this.benchmarksService.runById(runId))),
    {initialValue: undefined},
  );

  protected readonly loading = computed(() => this.run() === undefined);

  /** Все известные (эталонные) ошибки теста, найденные и пропущенные. */
  protected readonly expected = computed(() =>
    (this.run()?.results ?? [])
      .filter(result => result.expectedNumber != null)
      .sort((a, b) => (a.expectedNumber ?? 0) - (b.expectedNumber ?? 0)));

  /** «Лишние» ошибки: модель что-то нашла, но в тесте этого не было. */
  protected readonly extra = computed(() =>
    (this.run()?.results ?? [])
      .filter(result => result.expectedNumber == null)
      .sort((a, b) => (a.foundIndex ?? 0) - (b.foundIndex ?? 0)));

  /** Сколько известных ошибок модель не нашла. */
  protected readonly missed = computed(() =>
    this.expected().filter(result => !result.isFound).length);

  constructor() {
    // Прогон грузится по идентификатору из адреса; справочники нужны для ссылок.
    this.benchmarksService.ensureLoaded().pipe(
      takeUntilDestroyed(),
    ).subscribe();
  }

  protected percent(value: number): number {
    return Math.round(value * 100);
  }

  protected isActive(run: BenchmarkRunEntity): boolean {
    return run.status === ProgressStatus.Queued
      || run.status === ProgressStatus.InProgress
      || run.status === ProgressStatus.CancellationRequested;
  }

  protected isFailed(run: BenchmarkRunEntity): boolean {
    return run.status === ProgressStatus.Failed;
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

  /** Приоритет ошибки: 1 — самый важный. */
  protected priorityAppearance(priority?: number): string {
    if (priority == null)
      return 'neutral';
    if (priority <= 2)
      return 'negative';
    if (priority <= 5)
      return 'warning';
    return 'info';
  }

  protected matchMethodText(result: BenchmarkResultEntity): string {
    switch (result.matchingMethod) {
      case BenchmarkMatchMethod.Deterministic:
        return 'сопоставлено по главе и строке';
      case BenchmarkMatchMethod.Llm:
        return 'сопоставлено моделью-судьёй';
      default:
        return 'сопоставление не выполнялось';
    }
  }

  protected fixStatusText(status?: BenchmarkFixMatchStatus): string {
    switch (status) {
      case BenchmarkFixMatchStatus.Matched:
        return 'Исправление совпало';
      case BenchmarkFixMatchStatus.Mismatched:
        return 'Исправление отличается';
      case BenchmarkFixMatchStatus.MissingExpectedPatch:
        return 'Эталон без исправления';
      case BenchmarkFixMatchStatus.MissingFoundPatch:
        return 'Модель без исправления';
      default:
        return '';
    }
  }

  protected fixStatusAppearance(status?: BenchmarkFixMatchStatus): string {
    switch (status) {
      case BenchmarkFixMatchStatus.Matched:
        return 'positive';
      case BenchmarkFixMatchStatus.Mismatched:
        return 'warning';
      case BenchmarkFixMatchStatus.MissingExpectedPatch:
      case BenchmarkFixMatchStatus.MissingFoundPatch:
        return 'negative';
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

  /** Русская форма слова «запрос» для числа токенов. */
  protected requests(value: number): string {
    const mod10 = value % 10;
    const mod100 = value % 100;
    if (mod10 === 1 && mod100 !== 11)
      return `${value} запрос`;
    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14))
      return `${value} запроса`;
    return `${value} запросов`;
  }
}
