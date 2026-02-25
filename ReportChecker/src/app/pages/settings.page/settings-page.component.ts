import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton} from '@taiga-ui/core';
import {InstructionInput} from '../../components/instruction-input/instruction-input';
import {TUI_CONFIRM, TuiConfirmData} from '@taiga-ui/kit';
import {from, NEVER, switchMap} from 'rxjs';
import {Router} from '@angular/router';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {ReportsService} from '../../services/reports.service';

@Component({
  selector: 'app-instructions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    InstructionInput
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

  protected readonly instructions$ = this.instructionService.instructions$;

  ngOnInit() {
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
