import {ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {InstructionService} from '../../services/instruction.service';
import {InstructionEntity} from '../../entities/instruction-entity';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiTextarea, TuiTextareaLimit} from '@taiga-ui/kit';
import {TuiButton, TuiTextfield} from '@taiga-ui/core';

@Component({
  selector: 'app-instruction-input',
  imports: [
    TuiTextarea,
    ReactiveFormsModule,
    TuiButton,
    TuiTextfield,
    TuiTextareaLimit
  ],
  templateUrl: './instruction-input.html',
  styleUrl: './instruction-input.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstructionInput implements OnInit {
  private readonly instructionService = inject(InstructionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly instruction = input.required<InstructionEntity>();

  protected readonly control = new FormControl<string>("");

  ngOnInit() {
    this.control.setValue(this.instruction().content)
  }

  protected save() {
    console.log(this.control.value);
    this.instructionService.updateInstruction(this.instruction().id, this.control.value ?? "").pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected delete() {
    this.instructionService.deleteInstruction(this.instruction().id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
