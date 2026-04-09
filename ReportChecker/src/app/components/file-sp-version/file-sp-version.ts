import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {FileUploader} from '../file-uploader/file-uploader';
import {ReportsService} from '../../services/reports.service';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {first, NEVER, switchMap, tap, timer} from 'rxjs';
import {ApiClient} from '../../services/api-client';
import {TuiButton} from '@taiga-ui/core';

@Component({
  selector: 'app-file-sp-version',
  imports: [
    FileUploader,
    ReactiveFormsModule,
    TuiButton
  ],
  templateUrl: './file-sp-version.html',
  styleUrl: './file-sp-version.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileSpVersion implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly control = new FormControl<FileEntity | undefined>(undefined);

  ngOnInit() {
    this.control.valueChanges.pipe(
      switchMap(() => timer(1000)),
      switchMap(() => {
        if (this.control.value?.id)
          return this.reportsService.createCheck({
            fileName: this.control.value?.fileName,
          }, this.control.value?.id);
        return NEVER;
      }),
      tap(() => this.control.setValue(undefined)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected downloadFile() {
    this.reportsService.selectedReport$.pipe(
      first(),
      switchMap(report => {
        if (!report)
          return NEVER;
        return this.apiClient.filesGET(report.id);
      }),
      tap(resp => {
        if (resp?.url)
          window.location.href = resp.url;
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
