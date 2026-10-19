import {PatchLineEntity} from '../entities/patch-entity';

/** Строка исходного текста главы: номер строки и её содержимое. */
export interface SourceLineEntity {
  number: number;
  content?: string;
}

/** Вид изменения строки; значения совпадают с патчем в комментариях. */
export type FixLineType = 'Add' | 'Delete' | 'Modify';

/**
 * Сравнивает применённое исправление с исходным текстом главы и возвращает только
 * изменённые строки — так же, как это выглядит в патче под комментарием:
 * `Modify` заменяет строку, `Add` добавляет строку после указанной, `Delete` удаляет.
 * Исправления нет (`applied` пусто) или глава неизвестна — списка строк нет.
 */
export function diffFixLines(original: readonly SourceLineEntity[] | undefined,
                             applied?: string | null): PatchLineEntity[] {
  if (!original || !applied)
    return [];
  return buildLines(original, splitLines(applied));
}

/** Строки текста так же, как их делит сервер при применении патча. */
function splitLines(text: string): string[] {
  return text.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n');
}

interface Operation {
  kind: 'equal' | 'delete' | 'insert';
  text: string;
}

/**
 * Выравнивает строки исходного текста и применённого исправления по наибольшей общей
 * подпоследовательности. Подряд идущие удаления и вставки одинаковой длины считаются
 * заменой строки (`Modify`), лишние вставки — добавлением (`Add`), лишние удаления — `Delete`.
 */
function buildLines(original: readonly SourceLineEntity[], applied: string[]): PatchLineEntity[] {
  const operations = align(original.map(line => line.content ?? ''), applied);
  const lines: PatchLineEntity[] = [];
  // Сколько строк исходного текста уже пройдено: добавленная строка вставляется после них.
  let passed = 0;

  for (let index = 0; index < operations.length; index++) {
    const operation = operations[index];
    if (operation.kind === 'equal') {
      passed++;
      continue;
    }

    if (operation.kind === 'insert') {
      lines.push({number: numberBefore(original, passed), content: operation.text, type: 'Add'});
      continue;
    }

    const deleted: string[] = [];
    while (index < operations.length && operations[index].kind === 'delete')
      deleted.push(operations[index++].text);

    const inserted: string[] = [];
    while (index < operations.length && operations[index].kind === 'insert')
      inserted.push(operations[index++].text);
    index--;

    const replaced = Math.min(deleted.length, inserted.length);
    for (let pair = 0; pair < replaced; pair++, passed++) {
      const number = original[passed]?.number ?? passed + 1;
      lines.push({
        number,
        previousContent: deleted[pair],
        content: inserted[pair],
        type: 'Modify',
      });
    }
    for (let rest = replaced; rest < deleted.length; rest++, passed++) {
      const number = original[passed]?.number ?? passed + 1;
      lines.push({number, previousContent: deleted[rest], type: 'Delete'});
    }
    for (let rest = replaced; rest < inserted.length; rest++)
      lines.push({number: numberBefore(original, passed), content: inserted[rest], type: 'Add'});
  }

  return lines;
}

/** Номер строки, после которой вставляется новая; 0 — вставка в начало главы. */
function numberBefore(original: readonly SourceLineEntity[], passed: number): number {
  return passed === 0 ? 0 : original[passed - 1]?.number ?? passed;
}

function align(original: string[], applied: string[]): Operation[] {
  const rows = original.length;
  const columns = applied.length;
  // Общая длина подпоследовательности для пар (i, j); считаем с конца, чтобы восстановить путь.
  const lengths: Int32Array[] = Array.from({length: rows + 1}, () => new Int32Array(columns + 1));

  for (let i = rows - 1; i >= 0; i--) {
    for (let j = columns - 1; j >= 0; j--) {
      lengths[i][j] = original[i] === applied[j]
        ? lengths[i + 1][j + 1] + 1
        : Math.max(lengths[i + 1][j], lengths[i][j + 1]);
    }
  }

  const operations: Operation[] = [];
  let i = 0;
  let j = 0;
  while (i < rows && j < columns) {
    if (original[i] === applied[j]) {
      operations.push({kind: 'equal', text: original[i]});
      i++;
      j++;
    } else if (lengths[i + 1][j] >= lengths[i][j + 1]) {
      operations.push({kind: 'delete', text: original[i++]});
    } else {
      operations.push({kind: 'insert', text: applied[j++]});
    }
  }
  while (i < rows)
    operations.push({kind: 'delete', text: original[i++]});
  while (j < columns)
    operations.push({kind: 'insert', text: applied[j++]});

  return operations;
}
