import {ChangeDetectionStrategy, Component, DestroyRef, inject, input} from '@angular/core';
import {NEVER, switchMap, take, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiAlertService, TuiButton, TuiIcon, TuiNotification, TuiTitle} from '@taiga-ui/core';
import {IGoogleDriveReportSource} from '../../services/api-client';
import {ReportsService} from '../../services/reports.service';
import {GoogleDriveService} from '../../services/google-drive.service';

@Component({
  selector: 'app-google-drive-sp-version',
  imports: [
    TuiButton,
    TuiIcon,
    TuiNotification,
    TuiTitle,
  ],
  templateUrl: './google-drive-sp-version.html',
  styleUrl: './google-drive-sp-version.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoogleDriveSpVersion {
  private readonly reportsService = inject(ReportsService);
  private readonly googleDriveService = inject(GoogleDriveService);
  private readonly alerts = inject(TuiAlertService);
  private readonly destroyRef = inject(DestroyRef);

  source = input.required<IGoogleDriveReportSource | undefined | null>();

  protected sync() {
    this.reportsService.selectedReport$.pipe(
      take(1),
      switchMap(report => report ? this.googleDriveService.sync(report.id, true) : NEVER),
      tap(result => {
        if (result.created)
          this.alerts.open('Файл на Google Диске изменился — проверка запущена', {label: 'Google Диск'}).subscribe();
        else
          this.alerts.open(result.reason ?? 'Файл на Google Диске не изменился', {label: 'Google Диск'}).subscribe();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
