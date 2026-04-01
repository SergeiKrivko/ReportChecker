import {ChangeDetectionStrategy, Component} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../../entities/file-entity';
import {FileUploader} from '../../file-uploader/file-uploader';
import {SourceTester} from '../../source-tester/source-tester';
import {debounceTime, map, Observable} from 'rxjs';
import {AsyncPipe} from '@angular/common';
import {IFileReportSource} from '../../../services/api-client';
import {TuiLabel, TuiTextfieldComponent, TuiTextfieldDirective} from '@taiga-ui/core';

@Component({
  standalone: true,
  selector: 'app-file-sp-options',
  imports: [
    FileUploader,
    ReactiveFormsModule,
    SourceTester,
    AsyncPipe,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective
  ],
  templateUrl: './file-sp-options.html',
  styleUrl: './file-sp-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileSpOptions {
  protected readonly control = new FormGroup({
    file: new FormControl<FileEntity | null>(null),
    entryFilePath: new FormControl<string | null>(null),
  });

  protected isZip$ = this.control.valueChanges.pipe(
    map(e => e.file?.fileName.endsWith('.zip')),
  );

  protected readonly source$ : Observable<IFileReportSource> = this.control.valueChanges.pipe(
    debounceTime(1000),
    map(value => ({
      initialFileId: value?.file?.id ?? "",
      entryFilePath: value?.file ? value?.entryFilePath ?? undefined : undefined,
    })),
  );

  protected readonly reportName$ = this.control.valueChanges.pipe(
    map(value => value?.file?.fileName),
  );
}
