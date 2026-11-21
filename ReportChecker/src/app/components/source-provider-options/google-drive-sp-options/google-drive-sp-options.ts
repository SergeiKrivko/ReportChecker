import {ChangeDetectionStrategy, Component, DestroyRef, inject, signal} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {combineLatest, map, Observable, startWith} from 'rxjs';
import {TuiButton, TuiIcon, TuiLabel, TuiNotification, TuiTextfield} from '@taiga-ui/core';
import {TuiForm} from '@taiga-ui/layout';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {RouterLink} from '@angular/router';
import {SourceTester} from '../../source-tester/source-tester';
import {GoogleDriveService} from '../../../services/google-drive.service';
import {IGoogleDriveReportSource} from '../../../services/api-client';
import {
  GoogleDriveResourceEntity,
  isSupportedGoogleDriveFile,
} from '../../../entities/google-drive-entities';

interface GoogleDriveBreadcrumb {
  id: string;
  name: string;
}

@Component({
  standalone: true,
  selector: 'app-google-drive-sp-options',
  imports: [
    AsyncPipe,
    ReactiveFormsModule,
    RouterLink,
    TuiButton,
    TuiIcon,
    TuiLabel,
    TuiNotification,
    TuiTextfield,
    TuiForm,
    SourceTester,
  ],
  templateUrl: './google-drive-sp-options.html',
  styleUrl: './google-drive-sp-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoogleDriveSpOptions {
  private readonly googleDriveService = inject(GoogleDriveService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly connection$ = this.googleDriveService.connection();

  /** Стек пройденных папок: последний элемент — текущая папка. */
  protected readonly breadcrumbs = signal<GoogleDriveBreadcrumb[]>([{id: 'root', name: 'Мой диск'}]);
  protected readonly items = signal<GoogleDriveResourceEntity[]>([]);
  protected readonly nextPageToken = signal<string | undefined>(undefined);
  protected readonly loading = signal(false);

  protected readonly selected = signal<GoogleDriveResourceEntity | null>(null);
  protected readonly entryFilePath = new FormControl<string>('');

  protected readonly source$: Observable<IGoogleDriveReportSource | null> = combineLatest([
    toObservable(this.selected),
    this.entryFilePath.valueChanges.pipe(startWith('')),
  ]).pipe(
    map(([resource, entry]) => resource
      ? {fileId: resource.id, name: resource.name, entryFilePath: entry || undefined}
      : null),
  );

  protected readonly reportName$ = toObservable(this.selected).pipe(
    map(resource => resource?.name ?? null),
  );

  constructor() {
    this.load();
  }

  protected open(resource: GoogleDriveResourceEntity) {
    if (resource.type === 'dir') {
      this.breadcrumbs.update(breadcrumbs => [...breadcrumbs, {id: resource.id, name: resource.name}]);
      this.reset();
      return;
    }

    this.selected.set(resource);
    if (!resource.name.toLowerCase().endsWith('.zip'))
      this.entryFilePath.setValue('');
  }

  protected goTo(index: number) {
    if (index < 0 || index >= this.breadcrumbs().length - 1)
      return;
    this.breadcrumbs.update(breadcrumbs => breadcrumbs.slice(0, index + 1));
    this.reset();
  }

  protected up() {
    this.goTo(this.breadcrumbs().length - 2);
  }

  protected loadMore() {
    const pageToken = this.nextPageToken();
    if (pageToken)
      this.load(pageToken);
  }

  protected get isRoot(): boolean {
    return this.breadcrumbs().length <= 1;
  }

  protected get needsEntryFile(): boolean {
    const resource = this.selected();
    return resource?.type === 'dir' || (resource?.name.toLowerCase().endsWith('.zip') ?? false);
  }

  private reset() {
    this.selected.set(null);
    this.entryFilePath.setValue('');
    this.items.set([]);
    this.nextPageToken.set(undefined);
    this.load();
  }

  private load(pageToken?: string) {
    const breadcrumbs = this.breadcrumbs();
    const folderId = breadcrumbs[breadcrumbs.length - 1].id;
    this.loading.set(true);
    this.googleDriveService.resources(folderId, pageToken).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: folder => {
        // Ответ на устаревший запрос не должен перетирать содержимое текущей папки.
        const current = this.breadcrumbs();
        if (current[current.length - 1].id !== folderId)
          return;
        const items = folder.items.filter(item =>
          item.type === 'dir' || isSupportedGoogleDriveFile(item.name));
        this.items.update(current => pageToken ? [...current, ...items] : items);
        this.nextPageToken.set(folder.nextPageToken);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
