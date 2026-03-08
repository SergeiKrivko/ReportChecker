import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {map, NEVER, Observable, switchMap} from 'rxjs';
import {toObservable} from '@angular/core/rxjs-interop';
import {GithubService} from '../../services/github.service';
import {RepositoryInfoEntity} from '../../entities/github-entities';
import {AsyncPipe} from '@angular/common';
import {TuiNotification, TuiTitle} from '@taiga-ui/core';
import {TuiAvatar} from '@taiga-ui/kit';

interface GithubSourceSchema {
  RepositoryId?: number,
  BranchName?: string,
  FilePath?: string,
}

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

  source = input.required<string | undefined | null>();

  protected readonly source$: Observable<GithubSourceSchema | undefined> = toObservable(this.source).pipe(
    map(source => {
      if (!source)
        return undefined;
      return JSON.parse(source) as GithubSourceSchema;
    }),
  );

  protected readonly repositoryInfo$: Observable<RepositoryInfoEntity> = this.source$.pipe(
    switchMap(source => {
      if (!source?.RepositoryId)
        return NEVER;
      return this.githubService.getRepositoryInfo(source.RepositoryId);
    }),
  );
}
