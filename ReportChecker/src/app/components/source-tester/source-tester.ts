import {ChangeDetectorRef, Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {from, map, NEVER, switchMap, tap} from 'rxjs';
import {ReportsService} from '../../services/reports.service';
import {SourceInfoEntity} from '../../entities/source-info-entity';
import {TuiButton, TuiLabel, TuiNotification, TuiTextfield, TuiTextfieldComponent} from '@taiga-ui/core';
import {Router} from '@angular/router';
import {IFileReportSource, IGitHubReportSource} from '../../services/api-client';
import {AsyncPipe} from "@angular/common";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {TuiDataListWrapperComponent, TuiSelectDirective} from "@taiga-ui/kit";
import {LlmModelEntity} from '../../entities/llm-model-entity';

@Component({
  selector: 'app-source-tester',
  imports: [
    TuiNotification,
    TuiButton,
    AsyncPipe,
    ReactiveFormsModule,
    TuiDataListWrapperComponent,
    TuiLabel,
    TuiSelectDirective,
    TuiTextfieldComponent,
    TuiTextfield
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

  protected readonly control = new FormGroup({
    llmModel: new FormControl<LlmModelEntity | null>(null),
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
  }

  createReport() {
    const source = this.source();
    const format = this.sourceInfo?.format;
    if (!source || !format)
      return;
    const llmModelId = this.control.value.llmModel?.id;
    this.reportsService.createReport(this.name() || "New report", source, this.provider(), format, llmModelId).pipe(
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
}
