import {LlmReasoningEffort} from '../services/api-client';

/**
 * Уровни рассуждений в порядке возрастания «глубины» — для сортировки строк таблицы
 * и для списка выбора в интерфейсе.
 */
export const REASONING_EFFORT_OPTIONS: readonly LlmReasoningEffort[] = [
  LlmReasoningEffort.Default,
  LlmReasoningEffort.None,
  LlmReasoningEffort.Minimal,
  LlmReasoningEffort.Low,
  LlmReasoningEffort.Medium,
  LlmReasoningEffort.High,
  LlmReasoningEffort.XHigh,
  LlmReasoningEffort.Max,
];

/**
 * Уровень по умолчанию для новых прогонов: сервер без явного значения сохраняет `None`.
 */
export const DEFAULT_REASONING_EFFORT = LlmReasoningEffort.None;

const LABELS: Readonly<Record<LlmReasoningEffort, string>> = {
  [LlmReasoningEffort.Default]: 'По умолчанию',
  [LlmReasoningEffort.None]: 'Отключены',
  [LlmReasoningEffort.Minimal]: 'Минимальные',
  [LlmReasoningEffort.Low]: 'Низкие',
  [LlmReasoningEffort.Medium]: 'Средние',
  [LlmReasoningEffort.High]: 'Высокие',
  [LlmReasoningEffort.XHigh]: 'Очень высокие',
  [LlmReasoningEffort.Max]: 'Максимальные',
};

/** Пояснение к уровню там, где смысл не очевиден из названия. */
const HINTS: Partial<Readonly<Record<LlmReasoningEffort, string>>> = {
  [LlmReasoningEffort.Default]: 'настройка модели',
  [LlmReasoningEffort.None]: 'рассуждения выключены',
};

/** Человекочитаемое название уровня рассуждений. */
export function reasoningEffortText(effort?: LlmReasoningEffort | null): string {
  return effort ? LABELS[effort] ?? '—' : '—';
}

/** Короткое пояснение к уровню; пустая строка, если пояснять нечего. */
export function reasoningEffortHint(effort?: LlmReasoningEffort | null): string {
  return (effort && HINTS[effort]) || '';
}

/** Разбор уровня рассуждений из query-параметра; `null` — фильтр не задан. */
export function parseReasoningEffort(value?: string | null): LlmReasoningEffort | null {
  if (!value)
    return null;
  return REASONING_EFFORT_OPTIONS.find(effort => effort === value) ?? null;
}

/** Позиция уровня в списке — для устойчивой сортировки строк. */
export function reasoningEffortIndex(effort?: LlmReasoningEffort | null): number {
  const index = effort ? REASONING_EFFORT_OPTIONS.indexOf(effort) : -1;
  return index < 0 ? REASONING_EFFORT_OPTIONS.length : index;
}

/**
 * Ключ строки сводной таблицы: сводка группируется по тройке
 * «тест + модель + уровень рассуждений», поэтому строки различаются парой (модель, уровень).
 */
export function modelReasoningKey(modelId: string, effort?: LlmReasoningEffort | null): string {
  return `${modelId}|${effort ?? ''}`;
}
