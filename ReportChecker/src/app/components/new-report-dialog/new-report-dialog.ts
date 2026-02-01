import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {TuiFiles} from '@taiga-ui/kit';
import {tap} from 'rxjs';
import {AsyncPipe, NgIf} from '@angular/common';
import {injectContext} from '@taiga-ui/polymorpheus';
import {ApiClient} from '../../services/api-client';
import {TuiButton, TuiDialogContext} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {FileUploader} from '../file-uploader/file-uploader';
import {FileEntity} from '../../entities/file-entity';

@Component({
  selector: 'app-new-report-dialog',
  imports: [
    ReactiveFormsModule,
    TuiFiles,
    NgIf,
    AsyncPipe,
    TuiButton,
    FileUploader
  ],
  templateUrl: './new-report-dialog.html',
  styleUrl: './new-report-dialog.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewReportDialog {
  private readonly apiClient = inject(ApiClient);
  private readonly reportsService = inject(ReportsService);
  private readonly context = injectContext<TuiDialogContext>();

  protected readonly control = new FormControl<FileEntity | null>(null);

  createReport() {
    if (!this.control.value)
      return;
    this.reportsService.createReport(this.control.value.fileName, JSON.stringify(this.control.value)).pipe(
      tap(() => {
        this.context.completeWith();
      })
    ).subscribe();
  }
}
