import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {BehaviorSubject, combineLatest, map, Observable, startWith, switchMap, tap} from 'rxjs';
import {TuiButton, TuiIcon, TuiLabel, TuiNotification, TuiTextfield} from '@taiga-ui/core';
import {TuiForm} from '@taiga-ui/layout';
import {toObservable, takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ActivatedRoute} from '@angular/router';
import {TuiAlertService} from '@taiga-ui/core';
import {SourceTester} from '../../source-tester/source-tester';
import {YandexDiskService} from '../../../services/yandex-disk.service';
import {IYandexDiskReportSource} from '../../../services/api-client';
import {
  isSupportedYandexDiskFile,
  YandexDiskResourceEntity,
} from '../../../entities/yandex-disk-entities';

@Component({
  standalone: true,
  selector: 'app-yandex-disk-sp-options',
  imports: [
    AsyncPipe,
    ReactiveFormsModule,
    TuiButton,
    TuiIcon,
    TuiLabel,
    TuiNotification,
    TuiTextfield,
    TuiForm,
    SourceTester,
  ],
  templateUrl: './yandex-disk-sp-options.html',
  styleUrl: './yandex-disk-sp-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YandexDiskSpOptions implements OnInit {
  private readonly yandexDiskService = inject(YandexDiskService);
  private readonly alerts = inject(TuiAlertService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly connection$ = this.yandexDiskService.connection();
  protected readonly currentPath$ = new BehaviorSubject<string>('disk:/');
  protected readonly folder$ = this.currentPath$.pipe(
    switchMap(path => this.yandexDiskService.resources(path)),
    map(folder => ({
      ...folder,
      items: folder.items.filter(item =>
        item.type === 'dir' || isSupportedYandexDiskFile(item.name)),
    })),
  );

  protected readonly selected = signal<YandexDiskResourceEntity | null>(null);
  protected readonly entryFilePath = new FormControl<string>('');

  protected readonly source$: Observable<IYandexDiskReportSource | null> = combineLatest([
    toObservable(this.selected),
    this.entryFilePath.valueChanges.pipe(startWith('')),
  ]).pipe(
    map(([resource, entry]) => resource
      ? {path: resource.path, entryFilePath: entry || undefined}
      : null),
  );

  protected readonly reportName$ = toObservable(this.selected).pipe(
    map(resource => resource?.name ?? null),
  );

  ngOnInit() {
    this.activatedRoute.queryParamMap.pipe(
      tap(params => {
        const status = params.get('yandex');
        if (status === 'connected')
          this.alerts.open('Яндекс.Диск подключён', {label: 'Готово'}).subscribe();
        if (status === 'error')
          this.alerts.open('Не удалось подключить Яндекс.Диск', {
            label: 'Ошибка',
            appearance: 'negative',
          }).subscribe();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected connect() {
    this.yandexDiskService.oauthUrl().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(url => window.location.href = url);
  }

  protected open(resource: YandexDiskResourceEntity) {
    if (resource.type === 'dir') {
      this.currentPath$.next(resource.path);
      this.selected.set(null);
      this.entryFilePath.setValue('');
      return;
    }

    this.selected.set(resource);
    if (!resource.name.toLowerCase().endsWith('.zip'))
      this.entryFilePath.setValue('');
  }

  protected selectFolder(folder: string) {
    this.currentPath$.next(folder);
    this.selected.set(null);
    this.entryFilePath.setValue('');
  }

  protected up() {
    const path = this.currentPath$.value.replace(/\/$/, '');
    const index = path.lastIndexOf('/');
    this.selectFolder(index <= 5 ? 'disk:/' : path.substring(0, index));
  }

  protected get isRoot(): boolean {
    return this.currentPath$.value === 'disk:/';
  }

  protected get needsEntryFile(): boolean {
    const resource = this.selected();
    return resource?.type === 'dir' || (resource?.name.toLowerCase().endsWith('.zip') ?? false);
  }
}
