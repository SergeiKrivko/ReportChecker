import {Moment} from 'moment';
import {BenchmarkCaseDisplayMode} from '../services/api-client';

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
