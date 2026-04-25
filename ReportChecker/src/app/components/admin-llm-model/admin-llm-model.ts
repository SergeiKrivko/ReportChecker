import {Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {ApiClient, CreateLlmModelSchema, LlmModel} from '../../services/api-client';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {debounceTime, switchMap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiCardLarge} from '@taiga-ui/layout';
import {TuiAppearance, TuiLabel, TuiTextfield, TuiTextfieldComponent} from '@taiga-ui/core';
import {TuiInputNumber} from '@taiga-ui/kit';

@Component({
  selector: 'app-admin-llm-model',
  imports: [
    TuiCardLarge,
    TuiAppearance,
    ReactiveFormsModule,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfield,
    TuiInputNumber
  ],
  templateUrl: './admin-llm-model.html',
  styleUrl: './admin-llm-model.scss',
})
export class AdminLlmModel implements OnInit {
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);

  readonly model = input.required<LlmModel>();

  protected readonly control = new FormGroup({
    displayName: new FormControl<string>(""),
    modelKey: new FormControl<string>(""),
    inputCoefficient: new FormControl<number>(1),
    outputCoefficient: new FormControl<number>(1),
  });

  ngOnInit() {
    const model = this.model();
    this.control.setValue({
      displayName: model.displayName ?? "",
      modelKey: model.modelKey!,
      inputCoefficient: model.inputCoefficient ?? 1,
      outputCoefficient: model.outputCoefficient ?? 1,
    });

    this.control.valueChanges.pipe(
      debounceTime(1000),
      switchMap(value => this.apiClient.modelsPUT(this.model().id, CreateLlmModelSchema.fromJS(value))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
