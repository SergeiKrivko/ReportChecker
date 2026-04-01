import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input} from '@angular/core';
import {IssueAppearancePipe} from '../../pipes/issue-appearance-pipe';
import {IssueIconPipe} from '../../pipes/issue-icon-pipe';
import {TuiBadge, TuiBadgeNotification, TuiButtonLoading} from '@taiga-ui/kit';
import {TuiAppearance, TuiButton, TuiIcon} from '@taiga-ui/core';
import {IssuesService} from '../../services/issues.service';
import {IssueEntity} from '../../entities/issue-entity';
import {Subject, tap} from 'rxjs';
import {AsyncPipe} from '@angular/common';

@Component({
  selector: 'app-issue-header',
  imports: [
    IssueAppearancePipe,
    IssueIconPipe,
    TuiBadge,
    TuiButton,
    TuiIcon,
    TuiButtonLoading,
    AsyncPipe,
    TuiBadgeNotification,
    TuiAppearance
  ],
  templateUrl: './issue-header.html',
  styleUrl: './issue-header.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IssueHeader {
  private readonly issueService = inject(IssuesService);
  private readonly detectorRef = inject(ChangeDetectorRef);

  issue = input.required<IssueEntity>();

  protected loading = new Subject<boolean>();

  protected addComment(status: string | null = null) {
    this.loading.next(true);
    this.issueService.addIssueComment(this.issue().id, null, status).pipe(
      tap(() => this.loading.next(false)),
      tap(() => this.detectorRef.detectChanges()),
    ).subscribe();
  }

  protected getChapter() {
    return this.issue().chapter?.split('//').join(' • ');
  }

  protected commentsCount() {
    return this.issue().comments.filter(e => e.content).length;
  }
}
