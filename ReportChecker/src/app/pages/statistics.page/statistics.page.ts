import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {TuiHint, TuiPoint, TuiTextfield} from '@taiga-ui/core';
import {TuiInputDateRange} from '@taiga-ui/kit';
import {StatisticService} from '../../services/statistic.service';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {TuiDay, TuiDayRange} from '@taiga-ui/cdk';
import {combineLatest, map, Observable, tap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import moment from 'moment';
import {TuiAxes, TuiLineChart, TuiRingChart} from '@taiga-ui/addon-charts';
import {AsyncPipe} from '@angular/common';
import {AsDayPipe} from '../../pipes/as-day-pipe';
import {LlmModelEntity} from '../../entities/llm-model-entity';
import {ReportEntity} from '../../entities/report-entity';
import {ReportsService} from '../../services/reports.service';

@Component({
  selector: 'app-statistics.page',
  imports: [
    TuiTextfield,
    TuiInputDateRange,
    ReactiveFormsModule,
    TuiAxes,
    TuiLineChart,
    AsyncPipe,
    TuiHint,
    AsDayPipe,
    TuiRingChart
  ],
  templateUrl: './statistics.page.html',
  styleUrl: './statistics.page.scss',
})
export class StatisticsPage implements OnInit {
  private readonly statisticsService = inject(StatisticService);
  private readonly reportsService = inject(ReportsService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly control = new FormGroup({
    range: new FormControl<TuiDayRange>(new TuiDayRange(new TuiDay(2000, 1, 1), new TuiDay(2000, 1, 1))),
  });

  ngOnInit() {
    combineLatest([this.statisticsService.startDay$, this.statisticsService.endDay$]).pipe(
      tap(([startDay, endDay]) => {
        const tuiStartDay = TuiDay.fromUtcNativeDate(startDay.toDate());
        const tuiEndDay = TuiDay.fromUtcNativeDate(endDay.toDate());
        if (tuiStartDay.daySame(this.control.value.range?.from ?? new TuiDay(2000, 1, 1)))
          return;
        if (tuiEndDay.daySame(this.control.value.range?.to ?? new TuiDay(2000, 1, 1)))
          return;
        this.control.setValue({
          range: new TuiDayRange(tuiStartDay, tuiEndDay),
        });
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.control.valueChanges.pipe(
      tap(value => {
        const startDay = moment(value.range?.from.toUtcNativeDate());
        const endDay = moment(value.range?.to.toUtcNativeDate());
        this.statisticsService.selectRange(startDay, endDay);
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();

    this.reportsService.loadReports().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.reportsService.loadModels().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
    this.statisticsService.loadStatistics$.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected readonly timeChartData$: Observable<TuiPoint[]> = combineLatest([this.statisticsService.startDay$, this.statisticsService.timeChart$]).pipe(
    map(([startDate, data]) => data.map(e => [e.date.diff(startDate, 'days'), e.totalTokens])),
  );
  protected readonly timeWidth$: Observable<number> = combineLatest([this.statisticsService.startDay$, this.statisticsService.endDay$]).pipe(
    map(([startDay, endDay]) => endDay.diff(startDay, 'days')),
  );
  protected readonly timeHeight$: Observable<number> = this.statisticsService.timeChart$.pipe(
    map(intervals => intervals.map(e => e.totalTokens).reduce((m, c) => c > m ? c : m, 0)),
  );

  protected dateByX(x: number) {
    const startDate = moment(this.control.value.range?.from.toUtcNativeDate());
    return startDate.add(x, 'days');
  }

  private reportsChart: number[] = [];
  private modelsChart: number[] = [];
  private reports: ReportEntity[] = [];
  private models: LlmModelEntity[] = [];
  protected selectedReportIndex: number = 0;
  protected selectedModelIndex: number = 0;

  protected readonly reportChartData$: Observable<number[]> = this.statisticsService.barChart$.pipe(
    map(data => {
      const models: LlmModelEntity[] = [];
      const reports: ReportEntity[] = [];
      for (const datum of data) {
        if (datum.totalTokens == 0)
          continue;
        if (datum.model && !models.find(e => e.id === datum.model?.id))
          models.push(datum.model);
        if (datum.report && !reports.find(e => e.id === datum.report?.id))
          reports.push(datum.report);
      }

      const result: number[] = reports.map(_ => 0);
      for (const datum of data) {
        if (!datum.report)
          continue;
        result[reports.findIndex(e => e.id == datum.report?.id)] += datum.totalTokens;
      }

      this.models = models;
      this.reports = reports;
      this.reportsChart = result;

      return result;
    }),
  );

  protected readonly modelsChartData$: Observable<number[]> = this.statisticsService.barChart$.pipe(
    map(data => {
      const models: LlmModelEntity[] = [];
      const reports: ReportEntity[] = [];
      for (const datum of data) {
        if (datum.totalTokens == 0)
          continue;
        if (datum.model && !models.find(e => e.id === datum.model?.id))
          models.push(datum.model);
        if (datum.report && !reports.find(e => e.id === datum.report?.id))
          reports.push(datum.report);
      }

      const result: number[] = models.map(_ => 0);
      for (const datum of data) {
        if (!datum.report)
          continue;
        result[models.findIndex(e => e.id == datum.model?.id)] += datum.totalTokens;
      }

      this.models = models;
      this.reports = reports;
      this.modelsChart = result;

      return result;
    }),
  );

  protected getReportByIndex(index: number): ReportEntity | undefined {
    if (Number.isNaN(index))
      return undefined;
    return this.reports[index];
  }

  protected getReportTotalTokensByIndex(index: number): number {
    if (Number.isNaN(index))
      return this.reportsChart.reduce((acc, curr) => acc + curr, 0);
    return this.reportsChart[index];
  }

  protected getModelByIndex(index: number): LlmModelEntity | undefined {
    if (Number.isNaN(index))
      return undefined;
    return this.models[index];
  }

  protected getModelTotalTokensByIndex(index: number): number {
    if (Number.isNaN(index))
      return this.modelsChart.reduce((acc, curr) => acc + curr, 0);
    return this.modelsChart[index];
  }
}
