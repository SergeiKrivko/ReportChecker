import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs';
import {TuiAvatar} from '@taiga-ui/kit';
import {UserProfileService} from '../../services/user-profile.service';

@Component({
  selector: 'app-report-owner',
  imports: [
    AsyncPipe,
    TuiAvatar,
  ],
  templateUrl: './report-owner.html',
  styleUrl: './report-owner.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportOwner {
  private readonly userProfiles = inject(UserProfileService);

  readonly ownerId = input.required<string>();

  protected readonly owner$ = toObservable(this.ownerId).pipe(
    switchMap(ownerId => this.userProfiles.get(ownerId)),
  );
}
