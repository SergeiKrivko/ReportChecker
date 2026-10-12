import {Moment} from 'moment';
import {
  BenchmarkCaseDisplayMode,
  BenchmarkFixMatchStatus,
  BenchmarkMatchMethod,
  LlmReasoningEffort,
  ProgressStatus
} from '../services/api-client';

/**
 * Тест бенчмарка (колонка таблицы).
 */
export interface BenchmarkCaseEntity {
  id: string;
  name: string;
  description?: string;
  format?: string;
  displayMode?: BenchmarkCaseDisplayMode;
  /** Сколько известных (эталонных) ошибок в тесте. */
  expectedCount: number;
  /** Описание проблемы с загрузкой теста; null, если тест корректен. */
  validationError?: string;
}

/**
 * Один прогон бенчмарка: конкретная пара (тест, модель) в конкретный момент времени.
 */
export interface BenchmarkRunEntity {
  id: string;
  caseId: string;
  caseName: string;
  modelId: string;
  modelName: string;
  /** Уровень рассуждений, с которым выполнялся прогон. */
  reasoningEffort?: LlmReasoningEffort;
  status?: ProgressStatus;
  createdAt?: Moment;
  startedAt?: Moment;
  finishedAt?: Moment;
  /** Момент мягкого удаления; `undefined`, если прогон не удалён. */
  deletedAt?: Moment;
  durationMs?: number;
  failureReason?: string;

  expectedCount: number;
  foundCount: number;
  matchedCount: number;
  titleMatchCount: number;
  priorityMatchCount: number;
  fixCheckedCount: number;
  fixMatchCount: number;

  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  totalRequests: number;
  totalCost: number;

  /** Доля найденных известных ошибок — главный показатель прогона. */
  matchedShare: number;
  fixMatchShare: number;

  /** Результаты по каждой ошибке: известные (эталонные) и «лишние», найденные моделью. */
  results: BenchmarkResultEntity[];
}

/**
 * Результат по одной ошибке в рамках прогона.
 * Строка есть и на каждую известную ошибку, и на каждую «лишнюю», найденную моделью.
 */
export interface BenchmarkResultEntity {
  id: string;

  /** Номер известной ошибки; `undefined` для «лишних» ошибок. */
  expectedNumber?: number;
  errorClass?: string;
  chapter?: string;
  line?: number;

  expectedTitle?: string;
  expectedComment?: string;
  expectedPriority?: number;

  /** Порядковый номер найденной ошибки; `undefined`, если известная не найдена. */
  foundIndex?: number;
  foundTitle?: string;
  foundComment?: string;
  foundPriority?: number;

  isFound: boolean;
  titleMatch?: boolean;
  priorityMatch?: boolean;
  priorityDelta?: number;

  matchingMethod?: BenchmarkMatchMethod;
  matchScore?: number;
  matchingReason?: string;

  fixMatchStatus?: BenchmarkFixMatchStatus;
  expectedFix?: string;
  foundFix?: string;
}

/**
 * Агрегированный результат тройки (тест, модель, уровень рассуждений) —
 * summary по всем неудалённым прогонам этой группы.
 */
export interface BenchmarkSummaryEntity {
  caseId: string;
  modelId: string;
  /** Уровень рассуждений, с которым выполнялись прогоны группы. */
  reasoningEffort?: LlmReasoningEffort;
  caseName?: string;
  modelDisplayName?: string;

  runCount: number;
  completedRunCount: number;
  failedRunCount: number;
  cancelledRunCount: number;

  avgDurationMs?: number;
  minDurationMs?: number;
  maxDurationMs?: number;
  lastRunAt?: Moment;

  totalExpected: number;
  totalFound: number;
  totalMatched: number;
  totalExtra: number;
  totalTitleMatched: number;
  totalPriorityMatched: number;
  totalFixChecked: number;
  totalFixMatched: number;

  totalInputTokens: number;
  totalOutputTokens: number;
  totalTokens: number;
  totalRequests: number;
  totalCost: number;

  /** Главный показатель — доля найденных известных ошибок (recall). */
  matchedShare: number;
  titleMatchShare: number;
  priorityMatchShare: number;
  fixMatchShare: number;
}

/**
 * Строка сводной таблицы: одна пара «модель + уровень рассуждений» по горизонтали
 * против всех тестов по вертикали.
 */
export interface BenchmarkModelRowEntity {
  /** Ключ строки: модель вместе с уровнем рассуждений. */
  key: string;
  modelId: string;
  modelName: string;
  /** Уровень рассуждений строки; `undefined`, если уровень не задан. */
  reasoningEffort?: LlmReasoningEffort;
  /** Итоги по всем тестам, где у пары есть хотя бы один прогон. */
  totals: BenchmarkTotalsEntity;
  /** Результаты по тестам, ключ — идентификатор теста. */
  cells: Record<string, BenchmarkSummaryEntity>;
}

export interface BenchmarkTotalsEntity {
  totalExpected: number;
  totalFound: number;
  totalMatched: number;
  totalExtra: number;
  totalFixChecked: number;
  totalFixMatched: number;
  totalTokens: number;
  totalRequests: number;
  totalCost: number;
  runCount: number;
  /** Успешно завершённые прогоны: строка без них в таблицу не попадает. */
  completedRunCount: number;
  lastRunAt?: Moment;
  /**
   * Итоговый балл — среднее арифметическое `matchedShare` по тестам, где у пары
   * есть прогоны хотя бы с одной известной ошибкой. Тесты без известных ошибок
   * (`totalExpected === 0`) в среднее не входят: их балл не определён.
   */
  matchedShare: number;
  /** Число тестов, вошедших в средние (по ним и по `fixMatchShare`). */
  scoredCaseCount: number;
  /** Среднее арифметическое `fixMatchShare` по тестам с проверенными исправлениями. */
  fixMatchShare: number;
  /** Число тестов, вошедших в среднее по исправлениям (`totalFixChecked > 0`). */
  fixScoredCaseCount: number;
  /**
   * Средняя стоимость одного теста: среднее арифметическое средних стоимостей прогона
   * по каждому тесту. Суммы складывать нельзя — тест с большим числом прогонов
   * перевесил бы остальные, поэтому берётся среднее средних.
   */
  avgTestCost?: number;
  /** Число тестов, по которым посчитана средняя стоимость (`runCount > 0`). */
  costScoredCaseCount: number;
  avgDurationMs?: number;
}
