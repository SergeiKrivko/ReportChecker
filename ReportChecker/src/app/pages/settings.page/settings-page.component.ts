import {Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiTextfield} from '@taiga-ui/core';
import {InstructionInput} from '../../components/instruction-input/instruction-input';
import {
  TUI_CONFIRM,
  TuiBadge,
  TuiComboBox,
  TuiConfirmData,
  TuiDataListGroupWrapperComponent,
  TuiDataListWrapperComponent,
  TuiFilterByInputPipe,
  TuiSelectDirective
} from '@taiga-ui/kit';
import {combineLatest, debounceTime, EMPTY, filter, from, map, NEVER, Observable, switchMap, tap} from 'rxjs';
import {Router, RouterLink} from '@angular/router';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {ReportsService} from '../../services/reports.service';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {PathService} from '../../services/path.service';
import {LlmModelEntity} from '../../entities/llm-model-entity';
import {ImageProcessingModeEntity} from '../../entities/report-entity';
import {RoundPipe} from '../../pipes/round-pipe';
import {LlmModelGroupsPipe, LLM_MODEL_GROUP_LABELS} from '../../pipes/llm-model-groups-pipe';
import {LlmReasoningEffort} from '../../services/api-client';
import {REASONING_EFFORT_OPTIONS, reasoningEffortText} from '../../helpers/llm-reasoning-effort';

@Component({
  selector: 'app-instructions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    InstructionInput,
    TuiTextfield,
    ReactiveFormsModule,
    TuiDataListWrapperComponent,
    TuiDataListGroupWrapperComponent,
    TuiSelectDirective,
    TuiComboBox,
    TuiFilterByInputPipe,
    RouterLink,
    TuiBadge,
    RoundPipe,
    LlmModelGroupsPipe
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss',
  standalone: true
})
export class SettingsPage implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly dialogs = inject(TuiResponsiveDialogService);
  private readonly reportsService = inject(ReportsService);
  private readonly instructionService = inject(InstructionService);
  private readonly pathService = inject(PathService);

  protected readonly instructions$ = this.instructionService.instructions$;
  protected readonly models$ = this.reportsService.models$;

  protected readonly control = new FormGroup({
    name: new FormControl<string>(""),
    imageProcessingMode: new FormControl<ImageProcessingModeEntity>(ImageProcessingModeEntity.Disable),
    reasoningLevel: new FormControl<LlmReasoningEffort>(LlmReasoningEffort.Default),
  });

  /**
   * Отдельный контрол для комбобокса модели: при поиске tuiComboBox временно
   * сбрасывает значение в null, и если бы контрол входил в `control`,
   * это сохраняло бы отчет без модели.
   */
  protected readonly modelControl = new FormControl<LlmModelEntity | null>(null);

  /**
   * Последняя выбранная/сохраненная модель. Нужна, чтобы незавершенный поиск
   * в комбобоксе не сбрасывал модель отчета.
   */
  private savedModel: LlmModelEntity | null = null;

  protected readonly modelGroupLabels = LLM_MODEL_GROUP_LABELS;

  ngOnInit() {
    combineLatest([this.reportsService.selectedReport$, this.reportsService.models$]).pipe(
      tap(([report, models]) => {
        this.control.setValue({
          name: report?.name ?? "",
          imageProcessingMode: report?.imageProcessingMode ?? ImageProcessingModeEntity.Disable,
          reasoningLevel: report?.reasoningLevel ?? LlmReasoningEffort.Default,
        });
        this.savedModel = models.find(e => e.id == report?.llmModelId) ?? null;
        this.modelControl.setValue(this.savedModel, {emitEvent: false});
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.reportsService.loadModels().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.control.valueChanges.pipe(
      debounceTime(1000),
      switchMap(() => this.saveReport()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.modelControl.valueChanges.pipe(
      filter(model => model != null),
      tap(model => this.savedModel = model),
      switchMap(() => this.saveReport()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.instructionService.loadInstructionsOnReportChanged$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.pathService.add(this.reportsService.selectedReport$.pipe(
      map(report => {
        return {
          name: "Настройки",
          link: '/reports/' + report?.id,
          icon: "@tui.settings"
        }
      })
    ), 1);
  }

  ngOnDestroy() {
    this.pathService.clear(1);
  }

  private saveReport(): Observable<unknown> {
    const value = this.control.value;
    if (!value.name)
      return NEVER;
    return this.reportsService.updateReport(value.name, (this.modelControl.value ?? this.savedModel)?.id,
      value.imageProcessingMode ?? ImageProcessingModeEntity.Disable,
      value.reasoningLevel ?? LlmReasoningEffort.Default);
  }

  protected restoreModel() {
    if (this.modelControl.value == null)
      this.modelControl.setValue(this.savedModel, {emitEvent: false});
  }

  protected addInstruction() {
    this.instructionService.createInstruction().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected searchMoreIssues() {
    this.instructionService.tasks$.pipe(
      switchMap(tasks => {
        if (tasks.find(e => e.mode == 'SearchAny'))
          return EMPTY;
        return this.instructionService.createSearchAnyTask();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected deleteReport() {
    const data: TuiConfirmData = {
      content: 'Вы уверены, что хотите удалить этот отчет?',
      yes: 'Удалить',
      no: 'Отмена',
    };

    this.dialogs
      .open<boolean>(TUI_CONFIRM, {
        size: 's',
        data,
      }).pipe(
      switchMap((response) => {
        if (response)
          return this.reportsService.deleteSelectedReport();
        return NEVER;
      }),
      switchMap(() => from(this.router.navigate(['/']))),
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
