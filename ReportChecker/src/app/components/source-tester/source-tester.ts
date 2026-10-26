import {ChangeDetectorRef, Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {filter, from, map, NEVER, switchMap, tap} from 'rxjs';
import {ReportsService} from '../../services/reports.service';
import {SourceInfoEntity} from '../../entities/source-info-entity';
import {TuiButton, TuiLabel, TuiNotification, TuiTextfield, TuiTextfieldComponent} from '@taiga-ui/core';
import {Router} from '@angular/router';
import {IFileReportSource, IGitHubReportSource, LlmReasoningEffort} from '../../services/api-client';
import {AsyncPipe} from "@angular/common";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {
  TuiComboBox,
  TuiDataListGroupWrapperComponent,
  TuiDataListWrapperComponent,
  TuiFilterByInputPipe,
  TuiSelectDirective
} from "@taiga-ui/kit";
import {LlmModelEntity} from '../../entities/llm-model-entity';
import {ImageProcessingModeEntity} from '../../entities/report-entity';
import {REASONING_EFFORT_OPTIONS, reasoningEffortText} from '../../helpers/llm-reasoning-effort';
import {LlmModelGroupsPipe, LLM_MODEL_GROUP_LABELS} from '../../pipes/llm-model-groups-pipe';

@Component({
  selector: 'app-source-tester',
  imports: [
    TuiNotification,
    TuiButton,
    AsyncPipe,
    ReactiveFormsModule,
    TuiDataListWrapperComponent,
    TuiDataListGroupWrapperComponent,
    TuiLabel,
    TuiSelectDirective,
    TuiComboBox,
    TuiFilterByInputPipe,
    TuiTextfieldComponent,
    TuiTextfield,
    LlmModelGroupsPipe
  ],
  templateUrl: './source-tester.html',
  styleUrl: './source-tester.scss',
})
export class SourceTester implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  name = input<string | null>();
  source = input.required<IFileReportSource | IGitHubReportSource | null>();
  provider = input.required<string>();

  protected readonly source$ = toObservable(this.source);

  protected sourceInfo: SourceInfoEntity | undefined;
  protected readonly models$ = this.reportsService.models$;
  protected readonly modelGroupLabels = LLM_MODEL_GROUP_LABELS;

  private selectedModel: LlmModelEntity | null = null;

  protected readonly control = new FormGroup({
    llmModel: new FormControl<LlmModelEntity | null>(null),
    imageProcessingMode: new FormControl<ImageProcessingModeEntity>(ImageProcessingModeEntity.Disable),
    reasoningLevel: new FormControl<LlmReasoningEffort>(LlmReasoningEffort.Default),
  });

  ngOnInit() {
    this.source$.pipe(
      switchMap(source => {
        if (!source)
          return NEVER;
        return this.reportsService.testSource(this.provider(), source)
      }),
      tap(sourceInfo => {
        this.sourceInfo = sourceInfo;
        this.changeDetectorRef.detectChanges();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.reportsService.loadModels().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.control.controls.llmModel.valueChanges.pipe(
      filter((model): model is LlmModelEntity => model != null),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(model => this.selectedModel = model);
  }

  protected restoreModel() {
    if (this.control.value.llmModel == null)
      this.control.patchValue({llmModel: this.selectedModel}, {emitEvent: false});
  }

  createReport() {
    const source = this.source();
    const format = this.sourceInfo?.format;
    if (!source || !format)
      return;
    const llmModelId = this.control.value.llmModel?.id;
    const imageProcessingMode = this.control.value.imageProcessingMode;
    const reasoningLevel = this.control.value.reasoningLevel;
    this.reportsService.createReport(this.name() || "New report", source, this.provider(), format, llmModelId,
      imageProcessingMode ?? ImageProcessingModeEntity.Disable,
      reasoningLevel ?? LlmReasoningEffort.Default).pipe(
      switchMap(reportId => this.reportsService.loadReports().pipe(map(() => reportId))),
      switchMap(id => from(this.router.navigate(['/reports/' + id]))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected stringifyModel(model?: LlmModelEntity) {
    if (!model)
      return "По умолчанию"
    return model.displayName ?? "???";
  }

  protected readonly imageProcessingModes: ImageProcessingModeEntity[] = [
    ImageProcessingModeEntity.Disable,
    ImageProcessingModeEntity.Auto,
    ImageProcessingModeEntity.LowDetail,
    ImageProcessingModeEntity.HighDetail,
  ];

  protected stringifyImageProcessingMode(mode: ImageProcessingModeEntity): string {
    return imageProcessingModesMap[mode];
  }

  protected readonly reasoningEfforts = REASONING_EFFORT_OPTIONS;

  protected stringifyReasoningEffort(effort: LlmReasoningEffort): string {
    return reasoningEffortText(effort);
  }
}

const imageProcessingModesMap: Record<ImageProcessingModeEntity, string> = {
  [ImageProcessingModeEntity.Disable]: "Отключить",
  [ImageProcessingModeEntity.Auto]: "Автоматически",
  [ImageProcessingModeEntity.LowDetail]: "Низкая детализация",
  [ImageProcessingModeEntity.HighDetail]: "Высокая детализация",
}
