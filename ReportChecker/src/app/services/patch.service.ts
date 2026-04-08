import {inject, Injectable} from '@angular/core';
import {ApiClient, UpdatePatchSchema} from './api-client';
import {combineLatest, first, NEVER, Observable, switchMap} from 'rxjs';
import {ReportsService} from './reports.service';
import {IssuesService} from './issues.service';

@Injectable({
  providedIn: 'root',
})
export class PatchService {
  private readonly apiClient = inject(ApiClient);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);

  setPatchStatus(commentId: string, status: string): Observable<void> {
    return combineLatest([
      this.reportsService.selectedReport$,
      this.issuesService.selectedIssue$,
    ]).pipe(
      first(),
      switchMap(([report, issue]) => {
        if (!report || !issue)
          return NEVER;
        return this.apiClient.patch(report.id, issue.id, commentId, UpdatePatchSchema.fromJS({status}))
      }),
      switchMap(() => NEVER)
    );
  }
}
