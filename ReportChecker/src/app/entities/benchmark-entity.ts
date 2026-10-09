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
 * Агрегированный результат пары (тест, модель) — summary по всем неудалённым прогонам.
 */
export interface BenchmarkSummaryEntity {
  caseId: string;
  modelId: string;
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
 * Строка сводной таблицы: одна модель по горизонтали против всех тестов по вертикали.
 */
export interface BenchmarkModelRowEntity {
  modelId: string;
  modelName: string;
  /** Итоги по всем тестам, где у модели есть хотя бы один прогон. */
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
  lastRunAt?: Moment;
  matchedShare: number;
  fixMatchShare: number;
  avgDurationMs?: number;
}
