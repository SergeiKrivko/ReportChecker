import {ChangeDetectionStrategy, Component, input} from '@angular/core';
import {PatchLineEntity} from '../../entities/patch-entity';
import {DiffHtmlPipe} from '../../pipes/diff-html-pipe';

/**
 * Изменённые строки исправления — тот же вид, что и патч под комментарием:
 * `Add` показывает новую строку, `Delete` — удалённую, `Modify` — подсвеченную разницу.
 */
@Component({
  selector: 'app-fix-lines',
  imports: [DiffHtmlPipe],
  templateUrl: './fix-lines.html',
  styleUrl: './fix-lines.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FixLines {
  lines = input.required<PatchLineEntity[]>();
}
