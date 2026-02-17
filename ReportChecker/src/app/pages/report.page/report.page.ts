import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {map, Subject} from 'rxjs';
import {IssuesService} from '../../services/issues.service';
import {AsyncPipe} from '@angular/common';
import {TuiAccordion} from '@taiga-ui/experimental/components';
import {TuiButton, TuiGroup, TuiHint, TuiIcon, TuiLoader} from '@taiga-ui/core';
import {TuiAvatar, TuiBadge, TuiButtonLoading} from '@taiga-ui/kit';
import {IssueAppearancePipe} from '../../pipes/issue-appearance-pipe';
import {IssueIconPipe} from '../../pipes/issue-icon-pipe';
import {AvatarByUserIdPipe} from '../../pipes/avatar-by-user-id-pipe';
import {Comments} from '../../components/comments/comments';
import {FileUploader} from '../../components/file-uploader/file-uploader';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {IssueHeader} from '../../components/issue-header/issue-header';

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
    TuiHint,
    Comments,
    TuiButton,
    FileUploader,
    TuiButtonLoading,
    ReactiveFormsModule,
    TuiLoader,
    IssueHeader,
    RouterLink
  ],
  templateUrl: './report.page.html',
  styleUrl: './report.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportPage {
  private readonly issuesService = inject(IssuesService);

  protected readonly issues$ = this.issuesService.issues$.pipe(
    map(issues => issues.filter(e => e.status == "Open").sort((a, b) => a.priority - b.priority))
  );
  protected readonly closedIssues$ = this.issuesService.issues$.pipe(
    map(issues => issues.filter(e => e.status != "Open").sort((a, b) => a.priority - b.priority))
  );
  protected readonly isProgress$ = this.issuesService.isProgress$;

  protected readonly control = new FormControl<FileEntity | null>(null);

  protected loading = new Subject<boolean>();
}
