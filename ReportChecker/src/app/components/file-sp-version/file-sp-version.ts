import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {FileUploader} from '../file-uploader/file-uploader';
import {ReportsService} from '../../services/reports.service';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {NEVER, switchMap, tap, timer} from 'rxjs';

@Component({
  selector: 'app-file-sp-version',
  imports: [
    FileUploader,
    ReactiveFormsModule
  ],
  templateUrl: './file-sp-version.html',
  styleUrl: './file-sp-version.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileSpVersion implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly control = new FormControl<FileEntity | undefined>(undefined);

  ngOnInit() {
    this.control.valueChanges.pipe(
      switchMap(() => timer(1000)),
      switchMap(() => {
        if (this.control.value?.id)
          return this.reportsService.createCheck(JSON.stringify(this.control.value));
        return NEVER;
      }),
      tap(() => this.control.setValue(undefined)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
