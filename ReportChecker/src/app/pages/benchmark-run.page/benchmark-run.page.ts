import {ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {takeUntilDestroyed, toObservable, toSignal} from '@angular/core/rxjs-interop';
import {TuiAlertService, TuiButton, TuiLoader, TuiNotification} from '@taiga-ui/core';
import {TUI_CONFIRM, TuiBadge, TuiConfirmData} from '@taiga-ui/kit';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {catchError, distinctUntilChanged, filter, map, of, switchMap, tap} from 'rxjs';
import {BenchmarksService} from '../../services/benchmarks.service';
import {BenchmarkResultEntity, BenchmarkRunEntity} from '../../entities/benchmark-entity';
import {
  ApiClient,
  BenchmarkFixMatchStatus,
  BenchmarkMatchMethod,
  LlmReasoningEffort,
  ProgressStatus
} from '../../services/api-client';
import {PatchLineEntity} from '../../entities/patch-entity';
import {diffFixLines} from '../../helpers/fix-lines';
import {FixLines} from '../../components/fix-lines/fix-lines';
import {Moment} from 'moment';
import {AuthClient} from '../../auth/auth.client';

/**
 * Подробности одного прогона: сводка, все известные ошибки теста вместе с ответом модели
 * и отдельным списком «лишние» ошибки, которых в тесте не было.
 * Администратор может исключить прогон из результатов (мягкое удаление).
 */
@Component({
  selector: 'app-benchmark-run.page',
  imports: [
    RouterLink,
    TuiBadge,
    TuiButton,
    TuiLoader,
    TuiNotification,
    FixLines,
  ],
  templateUrl: './benchmark-run.page.html',
  styleUrl: './benchmark-run.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BenchmarkRunPage {
  private readonly benchmarksService = inject(BenchmarksService);
  private readonly apiClient = inject(ApiClient);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly authClient = inject(AuthClient);
  private readonly alerts = inject(TuiAlertService);
  private readonly dialogs = inject(TuiResponsiveDialogService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly runId$ = this.activatedRoute.paramMap.pipe(
    map(params => params.get('runId') ?? ''),
  );

  /**
   * `undefined` — прогон ещё загружается, `null` — не найден.
   * Запрашивается вместе с удалёнными: по прямой ссылке удалённый прогон открывается
   * с предупреждением, а не превращается в «не найден».
   */
  protected readonly run = toSignal(
    this.runId$.pipe(switchMap(runId => this.benchmarksService.runById(runId, true))),
    {initialValue: undefined},
  );

  protected readonly loading = computed(() => this.run() === undefined);

  /** Права администратора нужны для кнопки удаления; сервер проверяет их сам. */
  protected readonly isAdmin = toSignal(this.authClient.isAdmin$, {initialValue: false});

  /** Идёт удаление — кнопка блокируется, чтобы не отправить запрос дважды. */
  protected readonly deleting = signal(false);

  /** Прогон мягко удалён и исключён из списка прогонов и сводки. */
  protected readonly isDeleted = computed(() => this.run()?.deletedAt != null);

  /**
   * Исходный текст теста, разбитый на главы и строки; `undefined`, пока он грузится.
   * Номера строк совпадают с полем `line` результатов, поэтому по главе и номеру
   * восстанавливаются исходные строки для сравнения с предложенным исправлением.
   */
  protected readonly chapters = toSignal(
    toObservable(computed(() => this.run()?.caseId)).pipe(
      distinctUntilChanged(),
      switchMap(caseId => caseId
        ? this.apiClient.chapters(caseId).pipe(catchError(() => of(undefined)))
        : of(undefined)),
    ),
    {initialValue: undefined},
  );

  /** Строки исходного текста по названию главы — для сравнения с исправлением. */
  private readonly sourceLines = computed(() => {
    const map = new Map<string, {number: number; content?: string}[]>();
    for (const chapter of this.chapters()?.chapters ?? [])
      map.set(chapter.name ?? '', chapter.lines ?? []);
    return map;
  });

  /** Кнопка удаления доступна администратору и только для неудалённого прогона. */
  protected readonly canDelete = computed(() => this.isAdmin() && this.run() != null && !this.isDeleted());

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

  /**
   * Исключает прогон из результатов: подтверждение, затем мягкое удаление на сервере.
   * После успеха возвращаемся к списку — прогон уже не входит в сводку.
   */
  protected deleteRun() {
    const run = this.run();
    if (!run || this.deleting())
      return;

    const data: TuiConfirmData = {
      content: `Исключить прогон «${run.caseName}» (${run.modelName}) из результатов? `
        + 'Он пропадёт из списка прогонов и сводки, результаты останутся доступны по прямой ссылке.',
      yes: 'Удалить',
      no: 'Отмена',
    };

    this.dialogs.open<boolean>(TUI_CONFIRM, {size: 's', data}).pipe(
      filter(Boolean),
      tap(() => this.deleting.set(true)),
      switchMap(() => this.benchmarksService.deleteRun(run.id)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.deleting.set(false);
        this.alerts.open('Прогон исключён из результатов', {
          label: 'Прогон удалён',
          appearance: 'positive',
        }).subscribe();
        void this.router.navigate(['/benchmarks/runs']);
      },
      // Общий HTTP-слой уже показывает причину ошибки во всплывающем уведомлении.
      error: () => this.deleting.set(false),
    });
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

  /** Человекочитаемое название уровня рассуждений для прогона. */
  protected reasoningEffortText(effort?: LlmReasoningEffort): string {
    switch (effort) {
      case LlmReasoningEffort.Default:
        return 'По умолчанию';
      case LlmReasoningEffort.None:
        return 'Отключены';
      case LlmReasoningEffort.Minimal:
        return 'Минимальные';
      case LlmReasoningEffort.Low:
        return 'Низкие';
      case LlmReasoningEffort.Medium:
        return 'Средние';
      case LlmReasoningEffort.High:
        return 'Высокие';
      case LlmReasoningEffort.XHigh:
        return 'Очень высокие';
      case LlmReasoningEffort.Max:
        return 'Максимальные';
      default:
        return '—';
    }
  }

  protected fixStatusText(status?: BenchmarkFixMatchStatus): string {
    switch (status) {
      case BenchmarkFixMatchStatus.Matched:
        return 'Исправление совпало';
      case BenchmarkFixMatchStatus.Mismatched:
        return 'Исправление отличается';
      case BenchmarkFixMatchStatus.MissingExpectedPatch:
        return 'Исправление не ожидалось';
      case BenchmarkFixMatchStatus.MissingFoundPatch:
        return 'Исправление не предложено';
      default:
        return '';
    }
  }

  protected fixStatusAppearance(status?: BenchmarkFixMatchStatus): string {
    switch (status) {
      case BenchmarkFixMatchStatus.Matched:
        return 'positive';
      case BenchmarkFixMatchStatus.Mismatched:
        return 'negative';
      case BenchmarkFixMatchStatus.MissingExpectedPatch:
      case BenchmarkFixMatchStatus.MissingFoundPatch:
        return 'warning';
      default:
        return 'neutral';
    }
  }

  /** Изменённые строки эталонного исправления по сравнению с исходным текстом главы. */
  protected expectedFixLines(result: BenchmarkResultEntity): PatchLineEntity[] {
    return this.fixLines().get(result.id)?.expected ?? [];
  }

  /** Изменённые строки исправления модели по сравнению с исходным текстом главы. */
  protected foundFixLines(result: BenchmarkResultEntity): PatchLineEntity[] {
    return this.fixLines().get(result.id)?.found ?? [];
  }

  /**
   * Различия «исходный текст — применённое исправление» для каждой строки результатов.
   * Считается один раз на прогон: сравнение главы целиком слишком дорого для шаблона.
   */
  private readonly fixLines = computed(() => {
    const sourceLines = this.sourceLines();
    const map = new Map<string, { expected: PatchLineEntity[]; found: PatchLineEntity[] }>();
    for (const result of this.run()?.results ?? []) {
      const original = result.chapter ? sourceLines.get(result.chapter) : undefined;
      map.set(result.id, {
        expected: diffFixLines(original, result.expectedFix),
        found: diffFixLines(original, result.foundFix),
      });
    }
    return map;
  });

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
