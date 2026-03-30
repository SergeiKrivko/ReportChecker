import {inject, Injectable} from '@angular/core';
import {InstructionEntity} from '../entities/instruction-entity';
import {first, interval, map, NEVER, Observable, of, switchMap, takeWhile, tap, timer} from 'rxjs';
import {patchState, signalState} from '@ngrx/signals';
import {ApiClient, CreateInstructionTaskSchema, Instruction, InstructionTask} from './api-client';
import {ReportsService} from './reports.service';
import {toObservable} from '@angular/core/rxjs-interop';
import {InstructionTaskEntity} from '../entities/instruction-task-entity';
import {IssuesService} from './issues.service';

interface InstructionStore {
  instructions: InstructionEntity[];
  tasks: InstructionTaskEntity[];
}

@Injectable({
  providedIn: 'root',
})
export class InstructionService {
  private readonly apiClient = inject(ApiClient);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);

  private readonly store$$ = signalState<InstructionStore>({
    instructions: [],
    tasks: [],
  });
  readonly instructions$ = toObservable(this.store$$.instructions);
  readonly tasks$ = toObservable(this.store$$.tasks);

  private loadInstructions(reportId: string) {
    return this.apiClient.instructionsAll(reportId).pipe(
      tap(reports => {
        patchState(this.store$$, {
          instructions: reports.map(instructionToEntity),
        })
      }),
    );
  }

  loadInstructionsOnReportChanged$ = this.reportsService.selectedReport$.pipe(
    switchMap(report => {
      if (report)
        return this.loadInstructions(report.id);
      return NEVER;
    }),
    switchMap(() => NEVER),
  );

  createInstruction(content: string = "") {
    return this.reportsService.selectedReport$.pipe(
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
    return this.reportsService.selectedReport$.pipe(
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
    return this.reportsService.selectedReport$.pipe(
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

  createTask(id: string, mode: 'Apply' | 'Search') {
    return this.reportsService.selectedReport$.pipe(
      first(),
      switchMap(report => {
        if (report)
          return this.apiClient.tasks(report.id, CreateInstructionTaskSchema.fromJS({
            instructionId: id, mode
          })).pipe(
            switchMap(() => this.loadInstructions(report.id)),
          );
        return NEVER;
      }),
    );
  }

  loadTasks$: Observable<never> = this.reportsService.selectedReport$.pipe(
    switchMap(report => {
      if (!report) return of([]);

      let hasActiveTasks = false;

      // Базовый поллинг каждые 15 секунд
      return timer(0, 16000).pipe(
        switchMap(() => this.apiClient.tasksAll(report.id)),
        map(tasks => tasks.map(taskToEntity)),
        tap(tasks => {
          hasActiveTasks = tasks.length > 0;
          patchState(this.store$$, {tasks});
        }),
        // Если есть активные задачи - переключаемся на частый поллинг
        switchMap(tasks => {
          if (hasActiveTasks) {
            return interval(2000).pipe(
              switchMap(() => this.apiClient.tasksAll(report.id)),
              map(activeTasks => activeTasks.map(taskToEntity)),
              tap(activeTasks => {
                patchState(this.store$$, {tasks: activeTasks});
                // Продолжаем частый поллинг, пока есть задачи
                if (activeTasks.length === 0) {
                  hasActiveTasks = false;
                }
              }),
              takeWhile(() => hasActiveTasks, true),
              switchMap(() => this.issuesService.loadIssues(report.id)),
            );
          }
          return of(tasks);
        })
      );
    }),
    switchMap(() => NEVER),
  );

}

const instructionToEntity = (instruction: Instruction): InstructionEntity => ({
  id: instruction.id ?? "",
  reportId: instruction.reportId ?? "",
  content: instruction.content ?? "",
  createdAt: instruction.createdAt ?? null,
  deletedAt: instruction.deletedAt ?? null,
});

const taskToEntity = (task: InstructionTask): InstructionTaskEntity => ({
  id: task.id,
  reportId: task.reportId,
  status: task.status,
  mode: task.mode ?? "Apply",
  instruction: task.instruction,
});
