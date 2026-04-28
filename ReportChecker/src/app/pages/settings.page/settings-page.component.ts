import {Component, DestroyRef, inject, OnDestroy, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiTextfield} from '@taiga-ui/core';
import {InstructionInput} from '../../components/instruction-input/instruction-input';
import {TUI_CONFIRM, TuiConfirmData, TuiDataListWrapperComponent, TuiSelectDirective} from '@taiga-ui/kit';
import {combineLatest, debounceTime, from, map, NEVER, switchMap, tap} from 'rxjs';
import {Router} from '@angular/router';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {ReportsService} from '../../services/reports.service';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {PathService} from '../../services/path.service';
import {LlmModelEntity} from '../../entities/llm-model-entity';
import {ImageProcessingModeEntity} from '../../entities/report-entity';

@Component({
  selector: 'app-instructions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    InstructionInput,
    TuiTextfield,
    ReactiveFormsModule,
    TuiDataListWrapperComponent,
    TuiSelectDirective
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
    llmModel: new FormControl<LlmModelEntity | null>(null),
    imageProcessingMode: new FormControl<ImageProcessingModeEntity>(ImageProcessingModeEntity.Disable),
  });

  ngOnInit() {
    combineLatest([this.reportsService.selectedReport$, this.reportsService.models$]).pipe(
      tap(([report, models]) => this.control.setValue({
        name: report?.name ?? "",
        llmModel: models.find(e => e.id == report?.llmModelId) ?? null,
        imageProcessingMode: report?.imageProcessingMode ?? ImageProcessingModeEntity.Disable,
      })),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.reportsService.loadModels().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.control.valueChanges.pipe(
      debounceTime(1000),
      switchMap(value => {
        if (value.name)
          return this.reportsService.updateReport(value.name, value.llmModel?.id,
            value.imageProcessingMode ?? ImageProcessingModeEntity.Disable);
        return NEVER;
      }),
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

  protected addInstruction() {
    this.instructionService.createInstruction().pipe(
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
}

const imageProcessingModesMap: Record<ImageProcessingModeEntity, string> = {
  [ImageProcessingModeEntity.Disable]: "Отключить",
  [ImageProcessingModeEntity.Auto]: "Автоматически",
  [ImageProcessingModeEntity.LowDetail]: "Низкая детализация",
  [ImageProcessingModeEntity.HighDetail]: "Высокая детализация",
}
