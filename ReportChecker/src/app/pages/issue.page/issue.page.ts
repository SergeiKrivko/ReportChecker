import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit
} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {TuiButton, TuiIcon, TuiTextfield} from '@taiga-ui/core';
import {TuiBadge, TuiButtonLoading} from '@taiga-ui/kit';
import {IssuesService} from '../../services/issues.service';
import {ReactiveFormsModule} from '@angular/forms';
import {combineLatest, first, map, NEVER, of, Subject, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiLet} from '@taiga-ui/cdk';
import {Comments} from '../../components/comments/comments';
import {NextIssuePipe} from '../../pipes/next-issue-pipe';
import {PreviousIssuePipe} from '../../pipes/previous-issue-pipe';
import {ReportsService} from '../../services/reports.service';
import {PathService} from '../../services/path.service';
import {IssueEntity} from '../../entities/issue-entity';

@Component({
  selector: 'app-issue.page',
  imports: [
    RouterLink,
    TuiButton,
    AsyncPipe,
    TuiLet,
    ReactiveFormsModule,
    TuiTextfield,
    TuiButtonLoading,
    Comments,
    TuiBadge,
    TuiIcon,
    NextIssuePipe,
    PreviousIssuePipe,
  ],
  templateUrl: './issue.page.html',
  styleUrl: './issue.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IssuePage implements OnInit, OnDestroy {
  private readonly issueService = inject(IssuesService);
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly detectorRef = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);
  private readonly pathService = inject(PathService);

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

    this.issueService.markRead().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.pathService.add(combineLatest([
      this.reportsService.selectedReport$,
      this.issueService.selectedIssue$,
    ]).pipe(
      map(([report, issue]) => {
        return {
          name: issue?.title,
          link: '/reports/' + report?.id + '/issues/' + issue?.id,
          icon: issue ? this.issueIcon(issue) : undefined,
        }
      })
    ), 1)
  }

  ngOnDestroy() {
    this.pathService.clear(1);
  }

  issueIcon(issue: IssueEntity): string {
    if (issue.status == 'Open') {
      if (issue.priority >= 1 && issue.priority <= 2)
        return "@tui.shield-alert"
      if (issue.priority >= 3 && issue.priority <= 5)
        return "@tui.triangle-alert"
      return "@tui.circle-alert"
    }
    if (issue.status == 'Fixed')
      return "@tui.check"
    if (issue.status == 'Closed')
      return "@tui.x"
    return "@tui.circle-question-mark"
  }

  protected readonly selectedIssue$ = this.issueService.selectedIssue$;
  protected loading = new Subject<boolean>();
  protected readonly frozenStatus$ = this.issueService.selectedIssue$.pipe(
    first(e => e !== null),
    map(e => e?.status),
  );

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
