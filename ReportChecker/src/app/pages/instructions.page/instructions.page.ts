import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';
import {TuiButton} from '@taiga-ui/core';
import {InstructionInput} from '../../components/instruction-input/instruction-input';

@Component({
  selector: 'app-instructions.page',
  imports: [
    AsyncPipe,
    TuiButton,
    InstructionInput
  ],
  templateUrl: './instructions.page.html',
  styleUrl: './instructions.page.scss',
  standalone: true
})
export class InstructionsPage implements OnInit {
  private readonly instructionService = inject(InstructionService);
  private readonly destroyRef = inject(DestroyRef);

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
}
