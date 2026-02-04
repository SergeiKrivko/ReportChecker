import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {ReportsService} from '../../services/reports.service';
import {combineLatest, from, map, NEVER, Subject, switchMap, take, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {IssuesService} from '../../services/issues.service';
import {AsyncPipe} from '@angular/common';
import {TuiAccordion} from '@taiga-ui/experimental/components';
import {TuiButton, TuiGroup, TuiHint, TuiIcon, TuiLoader} from '@taiga-ui/core';
import {TUI_CONFIRM, TuiAvatar, TuiBadge, TuiButtonLoading, TuiConfirmData} from '@taiga-ui/kit';
import {IssueAppearancePipe} from '../../pipes/issue-appearance-pipe';
import {IssueIconPipe} from '../../pipes/issue-icon-pipe';
import {AvatarByUserIdPipe} from '../../pipes/avatar-by-user-id-pipe';
import {TuiInputModule} from '@taiga-ui/legacy';
import {Comments} from '../../components/comments/comments';
import {FileUploader} from '../../components/file-uploader/file-uploader';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';

@Component({
  selector: 'app-check.page',
  imports: [
    AsyncPipe,
    TuiAccordion,
    TuiIcon,
    TuiBadge,
    IssueAppearancePipe,
    IssueIconPipe,
    TuiAvatar,
    AvatarByUserIdPipe,
    TuiGroup,
    TuiInputModule,
    TuiHint,
    Comments,
    TuiButton,
    FileUploader,
    TuiButtonLoading,
    ReactiveFormsModule,
    TuiLoader
  ],
  templateUrl: './report.page.html',
  styleUrl: './report.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(TuiResponsiveDialogService);

  protected readonly issues$ = this.issuesService.issues$.pipe(
    map(issues => issues.filter(e => e.status == "Open").sort((a, b) => a.priority - b.priority))
  );
  protected readonly closedIssues$ = this.issuesService.issues$.pipe(
    map(issues => issues.filter(e => e.status != "Open").sort((a, b) => a.priority - b.priority))
  );
  protected readonly isProgress$ = this.issuesService.isProgress$;
  protected readonly selectedReport$ = this.reportsService.selectedReport$;

  protected readonly control = new FormControl<FileEntity | null>(null);

  ngOnInit() {
    combineLatest([
      this.route.params,
      this.reportsService.loaded$,
    ]).pipe(
      switchMap(([params, loaded]) => {
        const appId = params['id'];
        if (appId && loaded)
          return this.reportsService.selectReport(appId);
        return NEVER;
      }),
      take(1),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected loading = new Subject<boolean>();

  protected createCheck() {
    console.log("Creating check");
    console.log(this.control.value);
    if (!this.control.value)
      return;
    this.loading.next(true);
    this.reportsService.createCheck(JSON.stringify(this.control.value)).pipe(
      tap(() => this.loading.next(false))
    ).subscribe();
  }

  protected deleteReport() {
    const data: TuiConfirmData = {
      content: 'Вы уверены, что хотите удалить этот отчет?',
      yes: 'Удалить',
      no: 'Отмена',
    };

    this.dialogs
      .open<boolean>(TUI_CONFIRM, {
        size: 's',
        data,
      }).pipe(
        switchMap((response) => {
          if (response)
            return this.reportsService.deleteSelectedReport();
          return NEVER;
        }),
      switchMap(() => from(this.router.navigate(['/']))),
    ).subscribe();
  }
}
