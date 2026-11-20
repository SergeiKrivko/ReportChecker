import {ChangeDetectionStrategy, Component, DestroyRef, inject, input} from '@angular/core';
import {NEVER, switchMap, take, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiAlertService, TuiButton, TuiIcon, TuiNotification, TuiTitle} from '@taiga-ui/core';
import {IYandexDiskReportSource} from '../../services/api-client';
import {ReportsService} from '../../services/reports.service';
import {YandexDiskService} from '../../services/yandex-disk.service';

@Component({
  selector: 'app-yandex-disk-sp-version',
  imports: [
    TuiButton,
    TuiIcon,
    TuiNotification,
    TuiTitle,
  ],
  templateUrl: './yandex-disk-sp-version.html',
  styleUrl: './yandex-disk-sp-version.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YandexDiskSpVersion {
  private readonly reportsService = inject(ReportsService);
  private readonly yandexDiskService = inject(YandexDiskService);
  private readonly alerts = inject(TuiAlertService);
  private readonly destroyRef = inject(DestroyRef);

  source = input.required<IYandexDiskReportSource | undefined | null>();

  protected sync() {
    this.reportsService.selectedReport$.pipe(
      take(1),
      switchMap(report => report ? this.yandexDiskService.sync(report.id, true) : NEVER),
      tap(result => {
        if (result.created)
          this.alerts.open('Файл на Диске изменился — проверка запущена', {label: 'Яндекс.Диск'}).subscribe();
        else
          this.alerts.open(result.reason ?? 'Файл на Диске не изменился', {label: 'Яндекс.Диск'}).subscribe();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
