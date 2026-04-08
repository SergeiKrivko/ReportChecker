import {inject, Pipe, PipeTransform} from '@angular/core';
import {PatchLineEntity} from '../entities/patch-entity';
import {Diff, diff_match_patch} from 'diff-match-patch';
import {DomSanitizer, SafeHtml} from '@angular/platform-browser';

@Pipe({
  name: 'diffHtml',
})
export class DiffHtmlPipe implements PipeTransform {
  private readonly dmp = new diff_match_patch();
  private readonly sanitizer = inject(DomSanitizer);

  transform(patchLine?: PatchLineEntity): SafeHtml | undefined {
    if (!patchLine)
      return undefined;
    const diffs = this.dmp.diff_main(patchLine.previousContent ?? "", patchLine.content ?? "");
    this.dmp.diff_cleanupSemantic(diffs);

    // Преобразуем массив изменений в HTML
    const html = this.diffsToHtml(diffs);
    console.log(html);
    // Безопасно вставляем HTML
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  private diffsToHtml(diffs: Diff[]): string {
    let html = '';
    diffs.forEach(([op, data]) => {
      const text = data.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
      if (op === -1) { // Удалено (есть в старой, нет в новой)
        html += `<span class="diff-del">${text}</span>`;
      } else if (op === 1) { // Добавлено (нет в старой, есть в новой)
        html += `<span class="diff-ins">${text}</span>`;
      } else { // Без изменений
        html += text;
      }
    });
    return html;
  }

}
