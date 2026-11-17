import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input} from '@angular/core';
import {TuiAvatar, TuiButtonLoading, TuiTextarea} from '@taiga-ui/kit';
import {TuiButton, TuiIcon, TuiLoader, TuiTextfield} from '@taiga-ui/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {AsyncPipe} from '@angular/common';
import {IssueEntity} from '../../entities/issue-entity';
import {IssuesService} from '../../services/issues.service';
import {UserProfileService} from '../../services/user-profile.service';
import {Observable, Subject, tap} from 'rxjs';
import {OrderByCreatedAtPipe} from '../../pipes/order-by-created-at-pipe';
import {IsBotPipe} from '../../pipes/is-bot-pipe';
import {Patch} from '../patch/patch';
import {PublicUserEntity} from '../../entities/public-user-entity';

@Component({
  selector: 'app-comments',
  imports: [
    TuiAvatar,
    TuiTextfield,
    TuiIcon,
    TuiButton,
    ReactiveFormsModule,
    AsyncPipe,
    TuiButtonLoading,
    OrderByCreatedAtPipe,
    IsBotPipe,
    Patch,
    TuiTextarea,
    TuiLoader,
  ],
  templateUrl: './comments.html',
  styleUrl: './comments.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Comments {
  private readonly issueService = inject(IssuesService);
  private readonly userProfiles = inject(UserProfileService);
  private readonly detectorRef = inject(ChangeDetectorRef);

  issue = input.required<IssueEntity>();
  readOnly = input(false);

  protected readonly control = new FormControl<string>("");

  protected loading = new Subject<boolean>();

  protected avatar(userId: string): Observable<PublicUserEntity> {
    return this.userProfiles.get(userId);
  }

  protected addComment(status: string | null = null) {
    this.loading.next(true);
    const content = this.control.value;
    this.control.setValue("");
    this.issueService.addIssueComment(this.issue().id, content || null, status).pipe(
      tap(() => this.loading.next(false)),
      tap(() => this.detectorRef.detectChanges()),
    ).subscribe();
  }

  onKeydown(event: KeyboardEvent): void {
    // Отправка по Enter (без Shift)
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault(); // предотвращаем перенос строки
      this.addComment();
    }
    // При Shift+Enter - ничего не делаем, строка переносится автоматически
  }
}
