import {ChangeDetectionStrategy, Component, forwardRef, inject} from '@angular/core';
import {ApiClient} from '../../services/api-client';
import {ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule, Validators} from '@angular/forms';
import {TuiFileLike, TuiFiles} from '@taiga-ui/kit';
import {catchError, finalize, map, Observable, of, Subject, switchMap} from 'rxjs';
import {AsyncPipe, NgIf} from '@angular/common';
import {FileEntity} from '../../entities/file-entity';

@Component({
  selector: 'app-file-uploader',
  imports: [
    ReactiveFormsModule,
    TuiFiles,
    NgIf,
    AsyncPipe
  ],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => FileUploader),
      multi: true
    }
  ],
  templateUrl: './file-uploader.html',
  styleUrl: './file-uploader.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileUploader implements ControlValueAccessor {
  private readonly apiClient = inject(ApiClient);

  protected readonly control = new FormControl<TuiFileLike | null>(
    null,
    Validators.required,
  );

  protected readonly failedFiles$ = new Subject<TuiFileLike | null>();
  protected readonly loadingFiles$ = new Subject<TuiFileLike | null>();
  protected readonly loadedFiles$ = this.control.valueChanges.pipe(
    switchMap((file) => this.processFile(file)),
  );

  protected removeFile(): void {
    this.control.setValue(null);
  }

  private uploadedFile: FileEntity | undefined;

  protected processFile(file: TuiFileLike | null): Observable<TuiFileLike | null> {
    this.failedFiles$.next(null);

    if (this.control.invalid || !file) {
      return of(null);
    }

    this.loadingFiles$.next(file);

    return this.apiClient.files({fileName: file.name, data: file}).pipe(
      catchError(() => {
        this.failedFiles$.next(file);
        return of(null);
      }),
      map(schema => {
        if (schema) {
          this.uploadedFile = {
            id: schema.id,
            fileName: schema.fileName ?? "file",
          };
          this.onChange(this.readValue())
          return file;
        }
        this.failedFiles$.next(file);
        this.uploadedFile = undefined;
        this.onChange(this.readValue())
        return null;
      }),
      finalize(() => this.loadingFiles$.next(null)),
    );
  }

  private onChange: (value: FileEntity | null) => void = () => {
  };
  private onTouched: () => void = () => {
  };

  private readValue(): FileEntity | null {
    console.log("Reading value:", this.uploadedFile)
    return this.uploadedFile ?? null;
  }

  writeValue(value: FileEntity | null): void {
    this.uploadedFile = value ?? undefined;
    this.control.setValue(null);
  }

  registerOnChange(fn: (value: FileEntity | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    if (isDisabled)
      this.control.disable();
    else
      this.control.enable();
  }
}
