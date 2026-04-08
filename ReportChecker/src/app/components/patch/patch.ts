import {Component, computed, inject, input, Signal} from '@angular/core';
import {PatchEntity, PatchLineEntity, PatchStatusEntity} from '../../entities/patch-entity';
import {TuiAppearance, TuiAppearanceOptions, TuiButton, TuiLoader, TuiNotification, TuiScrollbar} from '@taiga-ui/core';
import {TuiCardLarge} from '@taiga-ui/layout';
import {ApiClient} from '../../services/api-client';
import {PatchService} from '../../services/patch.service';

@Component({
  selector: 'app-patch',
  imports: [
    TuiLoader,
    TuiNotification,
    TuiButton,
    TuiCardLarge,
    TuiAppearance,
    TuiScrollbar
  ],
  templateUrl: './patch.html',
  styleUrl: './patch.scss',
})
export class Patch {
  private readonly patchService = inject(PatchService);

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
        return 'primary';
    }
    return 'neutral';
  }

  protected acceptPatch() {
    this.patchService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Accepted).subscribe();
  }

  protected rejectPatch() {
    this.patchService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Rejected).subscribe();
  }

  protected readonly PatchStatusEntity = PatchStatusEntity;
}
