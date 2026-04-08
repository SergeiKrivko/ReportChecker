import {Component, computed, inject, input, Signal} from '@angular/core';
import {PatchEntity, PatchLineEntity, PatchStatusEntity} from '../../entities/patch-entity';
import {TuiAppearance, TuiAppearanceOptions, TuiButton, TuiLoader, TuiNotification, TuiScrollbar} from '@taiga-ui/core';
import {TuiCardLarge} from '@taiga-ui/layout';
import {IssuesService} from '../../services/issues.service';
import {DiffHtmlPipe} from '../../pipes/diff-html-pipe';

@Component({
  selector: 'app-patch',
  imports: [
    TuiLoader,
    TuiNotification,
    TuiButton,
    TuiCardLarge,
    TuiAppearance,
    TuiScrollbar,
    DiffHtmlPipe
  ],
  templateUrl: './patch.html',
  styleUrl: './patch.scss',
})
export class Patch {
  private readonly issuesService = inject(IssuesService);

  readonly patch = input.required<PatchEntity>();

  protected readonly lines: Signal<PatchLineEntity[]> = computed(() => {
    const patch = this.patch();
    const lines = patch.lines;
    return lines.sort((a, b) => a.number - b.number);
  });

  protected appearanceByLineType(lineType: string): TuiAppearanceOptions["appearance"] {
    switch (lineType) {
      case 'Add':
        return 'positive';
      case 'Delete':
        return 'negative';
      case 'Modify':
        return 'neutral';
    }
    return 'neutral';
  }

  protected acceptPatch() {
    this.issuesService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Accepted).subscribe();
  }

  protected rejectPatch() {
    this.issuesService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Rejected).subscribe();
  }

  protected readonly PatchStatusEntity = PatchStatusEntity;
}
