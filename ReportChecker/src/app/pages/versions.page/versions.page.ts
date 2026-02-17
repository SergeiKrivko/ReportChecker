import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {FileUploader} from '../../components/file-uploader/file-uploader';
import {TuiButton} from '@taiga-ui/core';
import {TuiButtonLoading} from '@taiga-ui/kit';
import {Subject, tap} from 'rxjs';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {ReportsService} from '../../services/reports.service';

@Component({
  selector: 'app-versions.page',
  imports: [
    AsyncPipe,
    FileUploader,
    TuiButton,
    TuiButtonLoading,
    ReactiveFormsModule
  ],
  templateUrl: './versions.page.html',
  styleUrl: './versions.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VersionsPage {
  private readonly reportsService = inject(ReportsService);

  protected readonly control = new FormControl<FileEntity | null>(null);
  protected loading = new Subject<boolean>();

  protected createCheck() {
    if (!this.control.value)
      return;
    this.loading.next(true);
    this.reportsService.createCheck(JSON.stringify(this.control.value)).pipe(
      tap(() => this.loading.next(false))
    ).subscribe();
  }
}
