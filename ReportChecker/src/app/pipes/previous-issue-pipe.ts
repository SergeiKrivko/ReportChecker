import {inject, Pipe, PipeTransform} from '@angular/core';
import {IssuesService} from '../services/issues.service';
import {map, Observable} from 'rxjs';
import {IssueEntity} from '../entities/issue-entity';

@Pipe({
  name: 'previousIssue',
  standalone: true
})
export class PreviousIssuePipe implements PipeTransform {
  private readonly issueService = inject(IssuesService);

  transform(issue: IssueEntity, status: string | null = null): Observable<IssueEntity | null> {
    return this.issueService.allIssues$.pipe(
      map(issues => {
        issues = issues
          .filter(e => e.id == issue.id || (e.status === 'Open') == ((status ?? issue.status) === 'Open'))
          .sort((a, b) => a.priority - b.priority);
        const index = issues.indexOf(issue);
        if (index <= 0)
          return null;
        return issues[index - 1];
      })
    );
  }

}
