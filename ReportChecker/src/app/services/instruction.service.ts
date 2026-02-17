import {inject, Injectable} from '@angular/core';
import {InstructionEntity} from '../entities/instruction-entity';
import {first, NEVER, switchMap, tap} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {ApiClient, Instruction} from './api-client';
import {AuthService} from './auth-service';
import {ReportsService} from './reports.service';
import {toObservable} from '@angular/core/rxjs-interop';

interface InstructionStore {
  instructions: InstructionEntity[];
}

@Injectable({
  providedIn: 'root',
})
export class InstructionService {
  private readonly apiClient = inject(ApiClient);
  private readonly authService = inject(AuthService);
  private readonly reportsService = inject(ReportsService);

  private readonly store$$ = signalState<InstructionStore>({
    instructions: [],
  });
  readonly instructions$ = toObservable(this.store$$.instructions);

  private loadInstructions(reportId: string) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.apiClient.instructionsAll(reportId)),
      tap(reports => {
        patchState(this.store$$, {
          instructions: reports.map(instructionToEntity),
        })
      }),
    );
  }

  loadInstructionsOnReportChanged$ = this.authService.refreshToken().pipe(
    switchMap(() => this.reportsService.selectedReport$),
    switchMap(report => {
      if (report)
        return this.loadInstructions(report.id);
      return NEVER;
    }),
    switchMap(() => NEVER),
  );

  createInstruction(content: string = "") {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.reportsService.selectedReport$),
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.instructionsPOST(report.id, content).pipe(
            switchMap(() => this.loadInstructions(report.id)),
          );
        return NEVER;
      }),
    );
  }

  updateInstruction(id: string, content: string) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.reportsService.selectedReport$),
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.instructionsPUT(report.id, id, content).pipe(
            switchMap(() => this.loadInstructions(report.id)),
          );
        return NEVER;
      }),
    );
  }

  deleteInstruction(id: string) {
    return this.authService.refreshToken().pipe(
      switchMap(() => this.reportsService.selectedReport$),
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.instructionsDELETE(report.id, id).pipe(
            switchMap(() => this.loadInstructions(report.id)),
          );
        return NEVER;
      }),
    );
  }

}

const instructionToEntity = (instruction: Instruction): InstructionEntity => ({
  id: instruction.id ?? "",
  reportId: instruction.reportId ?? "",
  content: instruction.content ?? "",
  createdAt: instruction.createdAt ?? null,
  deletedAt: instruction.deletedAt ?? null,
});
