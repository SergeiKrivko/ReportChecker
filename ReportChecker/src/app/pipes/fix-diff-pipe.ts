import {inject, Pipe, PipeTransform} from '@angular/core';
import {DomSanitizer, SafeHtml} from '@angular/platform-browser';
import {Diff, diff_match_patch} from 'diff-match-patch';

/**
 * Сравнивает эталонное исправление с предложенным моделью и подсвечивает различия.
 * Тексты — это целые главы, поэтому одинаковые фрагменты не раздувают разметку.
 */
@Pipe({
  name: 'fixDiff',
  standalone: true,
})
export class FixDiffPipe implements PipeTransform {
  private readonly dmp = new diff_match_patch();
  private readonly sanitizer = inject(DomSanitizer);

  transform(value?: { expected?: string; found?: string }): SafeHtml | undefined {
    const expected = value?.expected;
    const found = value?.found;

    if (!expected || !found)
      return undefined;

    const diffs = this.dmp.diff_main(expected, found);
    this.dmp.diff_cleanupSemantic(diffs);
    return this.sanitizer.bypassSecurityTrustHtml(this.toHtml(diffs));
  }

  private toHtml(diffs: Diff[]): string {
    return diffs
      .map(([operation, data]) => {
        const text = data
          .replace(/&/g, '&amp;')
          .replace(/</g, '&lt;')
          .replace(/>/g, '&gt;');
        if (operation === -1)
          return `<span class="diff-del">${text}</span>`;
        if (operation === 1)
          return `<span class="diff-ins">${text}</span>`;
        return text;
      })
      .join('');
  }
}
