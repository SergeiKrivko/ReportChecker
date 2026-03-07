import {inject, Injectable} from '@angular/core';
import {RepositoryEntity} from '../entities/github-entities';
import {ApiClient, Repository} from './api-client';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {AuthService} from './auth-service';
import {map, NEVER, switchMap, tap} from 'rxjs';

interface GitHubStore {
  repositories: RepositoryEntity[];
  selectedRepository: RepositoryEntity | null;
  branches: string[];
  selectedBranch: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class GithubService {
  private readonly apiClient = inject(ApiClient);
  private readonly authService = inject(AuthService);

  private readonly store$$ = signalState<GitHubStore>({
    repositories: [],
    selectedRepository: null,
    branches: [],
    selectedBranch: null,
  });

  readonly repositories$ = toObservable(this.store$$.repositories);
  readonly selectedRepository$ = toObservable(this.store$$.selectedRepository);
  readonly branches$ = toObservable(this.store$$.branches);
  readonly selectedBranch$ = toObservable(this.store$$.selectedBranch);

  readonly loadRepositories$ = this.authService.refreshToken().pipe(
    tap(() => patchState(this.store$$, {repositories: []})),
    switchMap(() => this.apiClient.repositories()),
    tap(resp => patchState(this.store$$, {repositories: resp.map(repositoryToEntity)})),
    switchMap(() => NEVER),
  );

  selectRepository(repository: RepositoryEntity) {
    patchState(this.store$$, {selectedRepository: repository});
  }

  readonly loadBranches$ = this.selectedRepository$.pipe(
    switchMap(repo => this.authService.refreshToken().pipe(map(() => repo))),
    switchMap(repo => {
      if (!repo)
        return NEVER;
      return this.apiClient.branches(repo.id);
    }),
    tap(resp => patchState(this.store$$, {branches: resp})),
    switchMap(() => NEVER),
  )

  selectBranch(branch: string) {
    patchState(this.store$$, {selectedBranch: branch});
  }
}

const repositoryToEntity = (repository: Repository): RepositoryEntity => {
  return {
    id: repository.id,
    name: repository.name ?? "Unknown",
  };
}
