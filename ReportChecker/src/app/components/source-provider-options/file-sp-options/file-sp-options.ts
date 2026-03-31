import {ChangeDetectionStrategy, Component} from '@angular/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../../entities/file-entity';
import {FileUploader} from '../../file-uploader/file-uploader';
import {SourceTester} from '../../source-tester/source-tester';
import {debounceTime, map, Observable} from 'rxjs';
import {AsyncPipe} from '@angular/common';
import {IFileReportSource} from '../../../services/api-client';

@Component({
  standalone: true,
  selector: 'app-file-sp-options',
  imports: [
    FileUploader,
    ReactiveFormsModule,
    SourceTester,
    AsyncPipe
  ],
  templateUrl: './file-sp-options.html',
  styleUrl: './file-sp-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileSpOptions {
  protected readonly control = new FormControl<FileEntity | null>(null);

  protected readonly source$ : Observable<IFileReportSource> = this.control.valueChanges.pipe(
    debounceTime(1000),
    map(value => ({
      initialFileId: value?.id ?? "",
    })),
  );

  protected readonly reportName$ = this.control.valueChanges.pipe(
    map(value => value?.fileName),
  );
}
