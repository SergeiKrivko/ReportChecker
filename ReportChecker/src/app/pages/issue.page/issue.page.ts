import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {ActivatedRoute, RouterLink, RouterLinkActive} from '@angular/router';
import {TuiButton, TuiGroup, TuiIcon, TuiLink, TuiScrollbar, TuiTextfield} from '@taiga-ui/core';
import {TuiAvatar, TuiBadge, TuiBreadcrumbs, TuiButtonLoading} from '@taiga-ui/kit';
import {IssuesService} from '../../services/issues.service';
import {ReactiveFormsModule} from '@angular/forms';
import {first, NEVER, of, Subject, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiLet} from '@taiga-ui/cdk';
import {OrderByCreatedAtPipe} from '../../pipes/order-by-created-at-pipe';
import {IssueHeader} from '../../components/issue-header/issue-header';
import {Comments} from '../../components/comments/comments';
import {NextIssuePipe} from '../../pipes/next-issue-pipe';
import {PreviousIssuePipe} from '../../pipes/previous-issue-pipe';
import {Header} from '../../components/header/header';
import {ReportsService} from '../../services/reports.service';
import {IssueIconPipe} from '../../pipes/issue-icon-pipe';

@Component({
  selector: 'app-issue.page',
  imports: [
    RouterLink,
    TuiButton,
    TuiAvatar,
    AsyncPipe,
    TuiLet,
    OrderByCreatedAtPipe,
    ReactiveFormsModule,
    TuiTextfield,
    TuiGroup,
    TuiButtonLoading,
    IssueHeader,
    Comments,
    TuiBadge,
    TuiIcon,
    NextIssuePipe,
    PreviousIssuePipe,
    Header,
    RouterLinkActive,
    TuiBreadcrumbs,
    TuiLink,
    TuiScrollbar,
    IssueIconPipe
  ],
  templateUrl: './issue.page.html',
  styleUrl: './issue.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IssuePage implements OnInit {
  private readonly issueService = inject(IssuesService);
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly detectorRef = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);

  ngOnInit() {
    this.route.params.pipe(
      switchMap(params => {
        const issueId = params['issueId'];
        if (issueId)
          return this.issueService.selectIssue(issueId);
        return NEVER;
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected readonly selectedIssue$ = this.issueService.selectedIssue$;
  protected readonly selectedReport$ = this.reportsService.selectedReport$;
  protected loading = new Subject<boolean>();

  protected addComment(status: string | null = null) {
    this.loading.next(true);
    this.selectedIssue$.pipe(
      first(),
      switchMap(issue => {
        if (issue)
          return this.issueService.addIssueComment(issue.id, null, status);
        return of(null);
      }),
      tap(() => this.loading.next(false)),
      tap(() => this.detectorRef.detectChanges()),
    ).subscribe();
  }
}
