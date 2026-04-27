import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {NEVER, Observable, switchMap} from 'rxjs';
import {toObservable} from '@angular/core/rxjs-interop';
import {GithubService} from '../../services/github.service';
import {RepositoryInfoEntity} from '../../entities/github-entities';
import {AsyncPipe} from '@angular/common';
import {TuiNotification, TuiTitle} from '@taiga-ui/core';
import {TuiAvatar} from '@taiga-ui/kit';
import {IGitHubReportSource} from '../../services/api-client';

@Component({
  selector: 'app-github-sp-version',
  imports: [
    AsyncPipe,
    TuiNotification,
    TuiTitle,
    TuiAvatar
  ],
  templateUrl: './github-sp-version.html',
  styleUrl: './github-sp-version.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GithubSpVersion {
  private readonly githubService = inject(GithubService);

  source = input.required<IGitHubReportSource | undefined | null>();

  protected readonly repositoryInfo$: Observable<RepositoryInfoEntity> = toObservable(this.source).pipe(
    switchMap(source => {
      if (!source?.repositoryId)
        return NEVER;
      return this.githubService.getRepositoryInfo(source.repositoryId);
    }),
  );
}
