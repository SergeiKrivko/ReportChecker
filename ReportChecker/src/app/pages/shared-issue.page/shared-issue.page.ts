import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {combineLatest, filter, map, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiButton, TuiIcon, TuiLabel, TuiLoader} from '@taiga-ui/core';
import {TuiBadge} from '@taiga-ui/kit';
import {SharedReportsService} from '../../services/shared-reports.service';
import {Comments} from '../../components/comments/comments';
import {IssueAppearancePipe} from '../../pipes/issue-appearance-pipe';
import {IssueIconPipe} from '../../pipes/issue-icon-pipe';
import {PathService} from '../../services/path.service';
import {IssueEntity} from '../../entities/issue-entity';

@Component({
  selector: 'app-shared-issue.page',
  imports: [
    AsyncPipe,
    RouterLink,
    TuiButton,
    TuiIcon,
    TuiLabel,
    TuiLoader,
    TuiBadge,
    Comments,
    IssueAppearancePipe,
    IssueIconPipe,
  ],
  templateUrl: './shared-issue.page.html',
  styleUrl: './shared-issue.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SharedIssuePage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sharedReportsService = inject(SharedReportsService);
  private readonly pathService = inject(PathService);

  protected readonly selectedIssue$ = this.sharedReportsService.selectedIssue$;
  protected readonly notFound = signal(false);

  protected readonly navigation$ = combineLatest([
    this.sharedReportsService.allIssues$,
    this.sharedReportsService.selectedIssue$,
  ]).pipe(
    map(([issues, issue]) => {
      if (!issue)
        return {previous: null as IssueEntity | null, next: null as IssueEntity | null};

      const filtered = issues
        .filter(e => e.id === issue.id || (e.status === 'Open') === (issue.status === 'Open'))
        .sort((a, b) => a.priority - b.priority);
      const index = filtered.findIndex(e => e.id === issue.id);

      return {
        previous: index > 0 ? filtered[index - 1] : null,
        next: index >= 0 && index < filtered.length - 1 ? filtered[index + 1] : null,
      };
    }),
  );

  ngOnInit() {
    combineLatest([
      this.route.params,
      this.sharedReportsService.loaded$,
    ]).pipe(
      filter(([, loaded]) => loaded),
      map(([params]) => params['issueId']),
      switchMap(issueId => this.sharedReportsService.selectIssue(issueId)),
      tap(issue => this.notFound.set(issue === null)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.pathService.add(combineLatest([
      this.sharedReportsService.token$,
      this.sharedReportsService.selectedIssue$,
    ]).pipe(
      map(([token, issue]) => ({
        name: issue?.title,
        link: `/shared/reports/${token}/issues/${issue?.id}`,
        icon: issue ? this.issueIcon(issue) : undefined,
      })),
    ), 1);
  }

  ngOnDestroy() {
    this.pathService.clear(1);
  }

  protected getChapter(issue: IssueEntity) {
    return issue.chapter?.split('//').join(' • ');
  }

  protected issueIcon(issue: IssueEntity): string {
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
}
