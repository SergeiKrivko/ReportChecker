import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiLink, TuiScrollbar, TuiTextfield} from '@taiga-ui/core';
import {InstructionInput} from '../../components/instruction-input/instruction-input';
import {TUI_CONFIRM, TuiBreadcrumbs, TuiConfirmData} from '@taiga-ui/kit';
import {debounceTime, from, NEVER, switchMap, tap} from 'rxjs';
import {Router, RouterLink} from '@angular/router';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {ReportsService} from '../../services/reports.service';
import {Header} from '../../components/header/header';
import {TuiItem} from '@taiga-ui/cdk';
import {FormControl, ReactiveFormsModule} from '@angular/forms';

@Component({
  selector: 'app-instructions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    InstructionInput,
    TuiScrollbar,
    Header,
    RouterLink,
    TuiBreadcrumbs,
    TuiLink,
    TuiItem,
    TuiTextfield,
    ReactiveFormsModule
  ],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss',
  standalone: true
})
export class SettingsPage implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly dialogs = inject(TuiResponsiveDialogService);
  private readonly reportsService = inject(ReportsService);
  private readonly instructionService = inject(InstructionService);

  protected readonly selectedReport$ = this.reportsService.selectedReport$;
  protected readonly instructions$ = this.instructionService.instructions$;

  protected readonly reportNameControl = new FormControl<string>("");

  ngOnInit() {
    this.reportsService.selectedReport$.pipe(
      tap(report => this.reportNameControl.setValue(report?.name ?? "")),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.reportNameControl.valueChanges.pipe(
      debounceTime(1000),
      switchMap(newName => {
        if (newName)
          return this.reportsService.renameReport(newName);
        return NEVER;
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.instructionService.loadInstructionsOnReportChanged$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
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
}
