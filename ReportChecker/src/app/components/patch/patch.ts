import {Component, computed, inject, input, Signal} from '@angular/core';
import {PatchEntity, PatchLineEntity, PatchStatusEntity} from '../../entities/patch-entity';
import {TuiButton, TuiLoader, TuiNotification} from '@taiga-ui/core';
import {IssuesService} from '../../services/issues.service';
import {FixLines} from '../fix-lines/fix-lines';

@Component({
  selector: 'app-patch',
  imports: [
    TuiLoader,
    TuiNotification,
    TuiButton,
    FixLines,
  ],
  templateUrl: './patch.html',
  styleUrl: './patch.scss',
})
export class Patch {
  private readonly issuesService = inject(IssuesService);

  readonly patch = input.required<PatchEntity>();
  readonly readOnly = input(false);

  protected readonly lines: Signal<PatchLineEntity[]> = computed(() =>
    [...this.patch().lines].sort((a, b) => a.number - b.number));

  protected acceptPatch() {
    this.issuesService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Accepted).subscribe();
  }

  protected rejectPatch() {
    this.issuesService.setPatchStatus(this.patch().commentId, PatchStatusEntity.Rejected).subscribe();
  }

  protected readonly PatchStatusEntity = PatchStatusEntity;
}
