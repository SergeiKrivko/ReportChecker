import {inject, Injectable} from '@angular/core';
import {ApiClient, LlmUsageGroup, LlmUsageInterval} from './api-client';
import moment, {Moment} from 'moment';
import {LlmUsageIntervalEntity} from '../entities/time-statistics-entity';
import {patchState, signalState} from '@ngrx/signals';
import {toObservable} from '@angular/core/rxjs-interop';
import {combineLatest, map, switchMap, tap} from 'rxjs';
import {LlmUsageGroupEntity} from '../entities/llm-usage-group-entity';
import {LlmModelEntity} from '../entities/llm-model-entity';
import {ReportEntity} from '../entities/report-entity';
import {ReportsService} from './reports.service';

interface StatisticsStore {
  startDay: Moment;
  endDay: Moment;
  timeChart: LlmUsageIntervalEntity[];
  usageGroups: LlmUsageGroupEntity[];
}

@Injectable({
  providedIn: 'root',
})
export class StatisticService {
  private readonly apiClient = inject(ApiClient);
  private readonly reportsService = inject(ReportsService);

  private readonly store$$ = signalState<StatisticsStore>({
    startDay: moment().startOf('day').add(-29, 'day').utc(),
    endDay: moment().startOf('day').add(1, 'day').utc(),
    timeChart: [],
    usageGroups: [],
  });

  readonly startDay$ = toObservable(this.store$$.startDay);
  readonly endDay$ = toObservable(this.store$$.endDay);
  readonly timeChart$ = toObservable(this.store$$.timeChart);
  readonly barChart$ = toObservable(this.store$$.usageGroups);

  selectRange(startDay: Moment, endDay: Moment) {
    patchState(this.store$$, {startDay, endDay});
  };

  loadStatistics$ = combineLatest([this.startDay$, this.endDay$, this.reportsService.reports$, this.reportsService.models$]).pipe(
    switchMap(([startDate, endDate, reports, models]) => {
      return combineLatest([
        this.apiClient.timeUsage(startDate, endDate, undefined, undefined, undefined).pipe(
          map(e => e.map(timeUsageToEntity)),
          tap(timeIntervals => {
            patchState(this.store$$, {timeChart: timeIntervals});
          })
        ),
        this.apiClient.usage(startDate, endDate).pipe(
          map(e => e.map(e => usageGroupToEntity(e, models, reports))),
          tap(groups => {
            patchState(this.store$$, {usageGroups: groups});
          })
        )
      ]);
    }),
  );
}

const timeUsageToEntity = (dto: LlmUsageInterval): LlmUsageIntervalEntity => ({
  date: dto.startTime,
  totalRequests: dto.totalRequests,
  totalTokens: dto.totalTokens,
});

const usageGroupToEntity = (dto: LlmUsageGroup, models?: LlmModelEntity[], reports?: ReportEntity[]): LlmUsageGroupEntity => ({
  model: models?.find(e => e.id == dto.modelId),
  report: reports?.find(e => e.id == dto.reportId),
  totalTokens: dto.totalTokens ?? 0,
  totalRequests: dto.totalRequests ?? 0,
});
