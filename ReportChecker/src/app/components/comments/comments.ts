import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input} from '@angular/core';
import {AvatarByUserIdPipe} from '../../pipes/avatar-by-user-id-pipe';
import {TuiAvatar, TuiButtonLoading, TuiTooltip} from '@taiga-ui/kit';
import {TuiButton, TuiGroup, TuiIcon, TuiTextfield} from '@taiga-ui/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {AsyncPipe} from '@angular/common';
import {IssueEntity} from '../../entities/issue-entity';
import {IssuesService} from '../../services/issues.service';
import {Subject, tap} from 'rxjs';
import {OrderByCreatedAtPipe} from '../../pipes/order-by-created-at-pipe';

@Component({
  selector: 'app-comments',
  imports: [
    AvatarByUserIdPipe,
    TuiAvatar,
    TuiGroup,
    TuiGroup,
    TuiTextfield,
    TuiIcon,
    TuiTooltip,
    TuiButton,
    ReactiveFormsModule,
    AsyncPipe,
    TuiButtonLoading,
    OrderByCreatedAtPipe
  ],
  templateUrl: './comments.html',
  styleUrl: './comments.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Comments {
  private readonly issueService = inject(IssuesService);
  private readonly detectorRef = inject(ChangeDetectorRef);

  issue = input.required<IssueEntity>();

  protected readonly control = new FormControl<string>("");

  protected loading = new Subject<boolean>();

  protected addComment(status: string | null = null) {
    this.loading.next(true);
    const content = this.control.value;
    this.control.setValue("");
    this.issueService.addIssueComment(this.issue().id, content || null, status).pipe(
      tap(() => this.loading.next(false)),
      tap(() => this.detectorRef.detectChanges()),
    ).subscribe();
  }
}
