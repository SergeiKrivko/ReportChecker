import {inject, Injectable} from '@angular/core';
import {RepositoryEntity, RepositoryInfoEntity} from '../entities/github-entities';
import {ApiClient, Repository, RepositoryInfo} from './api-client';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
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

  readonly loadRepositories$ = this.apiClient.repositoriesAll().pipe(
    tap(resp => patchState(this.store$$, {repositories: resp.map(repositoryToEntity)})),
    switchMap(() => NEVER),
  );

  selectRepository(repository: RepositoryEntity) {
    patchState(this.store$$, {selectedRepository: repository});
  }

  readonly loadBranches$ = this.selectedRepository$.pipe(
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

  getRepositoryInfo(id: number) {
    return this.apiClient.repositories(id).pipe(
      map(repositoryInfoToEntity),
    );
  }
}

const repositoryToEntity = (repository: Repository): RepositoryEntity => {
  return {
    id: repository.id,
    name: repository.name ?? "Unknown",
  };
}

const repositoryInfoToEntity = (repository: RepositoryInfo): RepositoryInfoEntity => {
  return {
    id: repository.id,
    name: repository.name ?? "Unknown",
    url: repository.url,
  };
}
