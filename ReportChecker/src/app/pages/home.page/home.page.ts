import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {NewReportDialog} from '../../components/new-report-dialog/new-report-dialog';
import {TuiButton, tuiDialog, TuiScrollbar, TuiSurface} from '@taiga-ui/core';
import {ReportsService} from '../../services/reports.service';
import {AsyncPipe} from '@angular/common';
import {TuiCardLarge, TuiHeader} from '@taiga-ui/layout';
import {RouterLink} from '@angular/router';
import {Header} from '../../components/header/header';

@Component({
  selector: 'app-home.page',
  imports: [
    TuiButton,
    AsyncPipe,
    TuiCardLarge,
    TuiHeader,
    TuiSurface,
    RouterLink,
    Header,
    TuiScrollbar
  ],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly reportService = inject(ReportsService);

  protected readonly reports$ = this.reportService.reports$;

  private readonly newReportDialog = tuiDialog(NewReportDialog, {
    dismissible: false,
    label: 'Новый отчет',
  });

  protected newReport(): void {
    this.newReportDialog(undefined).subscribe();
  }
}
