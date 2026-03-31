import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {TuiForm} from '@taiga-ui/layout';
import {TuiButton, TuiTextfield} from '@taiga-ui/core';
import {TuiDataListWrapper, TuiSelect} from '@taiga-ui/kit';
import {RepositoryEntity} from '../../../entities/github-entities';
import {GithubService} from '../../../services/github.service';
import {AsyncPipe} from '@angular/common';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {combineLatest, debounceTime, map, Observable, tap} from 'rxjs';
import {SourceTester} from '../../source-tester/source-tester';
import {IGitHubReportSource} from '../../../services/api-client';

@Component({
  standalone: true,
  selector: 'app-github-sp-options',
  imports: [
    TuiForm,
    TuiTextfield,
    FormsModule,
    ReactiveFormsModule,
    TuiSelect,
    TuiDataListWrapper,
    AsyncPipe,
    SourceTester,
    TuiButton
  ],
  templateUrl: './github-sp-options.html',
  styleUrl: './github-sp-options.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GithubSpOptions implements OnInit {
  private readonly githubService = inject(GithubService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit() {
    combineLatest([this.githubService.loadRepositories$, this.githubService.loadBranches$]).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.control.get('repository')?.valueChanges.pipe(
      tap(e => {
        if (e)
          this.githubService.selectRepository(e)
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected readonly repositories$ = this.githubService.repositories$;
  protected readonly branches$ = this.githubService.branches$;
  protected readonly installed$ = this.githubService.checkInstallation();

  protected readonly control = new FormGroup({
    repository: new FormControl<RepositoryEntity | null>(null),
    branch: new FormControl<string | null>(null),
    path: new FormControl<string>(""),
  });

  protected readonly source$: Observable<IGitHubReportSource> = this.control.valueChanges.pipe(
    debounceTime(1000),
    map(value => ({
      repositoryId: value.repository?.id ?? 0,
      branch: value.branch ?? "",
      path: value.path ?? "",
    })),
  );

  protected readonly reportName$ = this.control.valueChanges.pipe(
    map(value => value.repository?.name),
  );

  protected stringifyRepository(repository: RepositoryEntity) {
    return repository.name;
  }
}
