import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiHint, TuiLoader} from '@taiga-ui/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {BenchmarksService} from '../../services/benchmarks.service';
import {BenchmarkCaseEntity} from '../../entities/benchmark-entity';
import {BenchmarkRunner} from '../../components/benchmark-runner/benchmark-runner';
import {AuthClient} from '../../auth/auth.client';
import {Observable} from 'rxjs';

@Component({
  selector: 'app-benchmarks.page',
  imports: [
    AsyncPipe,
    BenchmarkRunner,
    TuiButton,
    TuiHint,
    TuiLoader,
  ],
  templateUrl: './benchmarks.page.html',
  styleUrl: './benchmarks.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BenchmarksPage implements OnInit {
  private readonly benchmarksService = inject(BenchmarksService);
  private readonly authClient = inject(AuthClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly cases$ = this.benchmarksService.cases$;
  protected readonly rows$ = this.benchmarksService.rows$;
  protected readonly loaded$ = this.benchmarksService.loaded$;
  protected readonly error$ = this.benchmarksService.error$;

  /** Панель запуска показывается только администратору; сервер проверяет права сам. */
  protected readonly isAdmin$: Observable<boolean> = this.authClient.isAdmin$;

  ngOnInit() {
    this.load();
  }

  protected load() {
    this.benchmarksService.load().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  /** Доля найденных известных ошибок в процентах — главный показатель. */
  protected percent(share?: number): number {
    return Math.round((share ?? 0) * 100);
  }

  /**
   * Уровень показателя для подсветки: чем больше найденных известных ошибок, тем лучше.
   */
  protected level(share: number | undefined, expected: number): string {
    if (expected === 0)
      return 'empty';
    const value = share ?? 0;
    if (value >= 0.9)
      return 'excellent';
    if (value >= 0.75)
      return 'good';
    if (value >= 0.5)
      return 'fair';
    return 'poor';
  }

  /** Подсказка к заголовку теста. */
  protected caseHint(benchmarkCase: BenchmarkCaseEntity): string {
    if (benchmarkCase.validationError)
      return `Тест не загружен: ${benchmarkCase.validationError}`;
    const lines = [`Известных ошибок: ${benchmarkCase.expectedCount}`];
    if (benchmarkCase.format)
      lines.push(`Формат: ${benchmarkCase.format}`);
    if (benchmarkCase.description)
      lines.push(benchmarkCase.description);
    return lines.join('\n');
  }

  protected duration(ms?: number): string {
    if (ms == null)
      return '—';
    if (ms < 1000)
      return `${ms} мс`;
    return `${(ms / 1000).toFixed(1)} с`;
  }

  protected tokens(value?: number): string {
    if (value == null)
      return '—';
    if (value < 1000)
      return `${value}`;
    return `${(value / 1000).toFixed(value < 10000 ? 1 : 0)} тыс.`;
  }

  protected cost(value?: number): string {
    if (value == null || value === 0)
      return '—';
    return `${value.toFixed(2)} ₽`;
  }
}
