import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {TuiFiles} from '@taiga-ui/kit';
import {from, map, switchMap, tap} from 'rxjs';
import {AsyncPipe, NgIf} from '@angular/common';
import {injectContext} from '@taiga-ui/polymorpheus';
import {TuiButton, TuiDialogContext} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {FileUploader} from '../file-uploader/file-uploader';
import {FileEntity} from '../../entities/file-entity';
import {Router} from '@angular/router';

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
  private readonly reportsService = inject(ReportsService);
  private readonly router = inject(Router);
  private readonly context = injectContext<TuiDialogContext>();

  protected readonly control = new FormControl<FileEntity | null>(null);

  createReport() {
    if (!this.control.value)
      return;
    let format: string = "";
    if (this.control.value.fileName.endsWith(".zip"))
      format = "Latex";
    else if (this.control.value.fileName.endsWith(".pdf"))
      format = "Pdf";

    this.reportsService.createReport(this.control.value.fileName, JSON.stringify(this.control.value), format).pipe(
      switchMap(reportId => this.reportsService.loadReports().pipe(map(() => reportId))),
      switchMap(reportId => from(this.router.navigate(['reports/' + reportId]))),
      tap(() => {
        this.context.completeWith();
      }),
    ).subscribe();
  }
}
