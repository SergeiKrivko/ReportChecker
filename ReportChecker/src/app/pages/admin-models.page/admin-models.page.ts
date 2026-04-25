import {Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {ApiClient, CreateLlmModelSchema, LlmModel} from '../../services/api-client';
import {BehaviorSubject, Observable, of, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {AdminLlmModel} from '../../components/admin-llm-model/admin-llm-model';
import {TuiButton} from '@taiga-ui/core';
import {PathService} from '../../services/path.service';

@Component({
  selector: 'app-admin-models.page',
  imports: [
    AsyncPipe,
    AdminLlmModel,
    TuiButton
  ],
  templateUrl: './admin-models.page.html',
  styleUrl: './admin-models.page.scss',
})
export class AdminModelsPage implements OnInit, OnDestroy {
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pathService = inject(PathService);

  private readonly store$$ = new BehaviorSubject<LlmModel[]>([]);
  protected readonly models$: Observable<LlmModel[]> = this.store$$;

  ngOnInit() {
    this.loadModels().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.pathService.add(of({name: "Модели", link: "/admin/models"}), 1);
  }

  ngOnDestroy() {
    this.pathService.clear(1);
  }

  private loadModels() {
    return this.apiClient.modelsAll().pipe(
      tap((models: LlmModel[]) => this.store$$.next(models)),
    );
  }

  protected addModel() {
    this.apiClient.modelsPOST(CreateLlmModelSchema.fromJS({
      displayName: "New model",
      modelKey: "model-key",
      inputCoefficient: 1,
      outputCoefficient: 1
    })).pipe(
      switchMap(() => this.loadModels()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
