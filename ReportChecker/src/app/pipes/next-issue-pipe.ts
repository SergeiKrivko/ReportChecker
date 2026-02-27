import {inject, Pipe, PipeTransform} from '@angular/core';
import {IssuesService} from '../services/issues.service';
import {map, Observable} from 'rxjs';
import {IssueEntity} from '../entities/issue-entity';

@Pipe({
  name: 'nextIssue',
  standalone: true
})
export class NextIssuePipe implements PipeTransform {
  private readonly issueService = inject(IssuesService);

  transform(issue: IssueEntity): Observable<IssueEntity | null> {
    return this.issueService.issues$.pipe(
      map(issues => {
        issues = issues
          .filter(e => (e.status === 'Open') == (issue.status === 'Open'))
          .sort((a, b) => a.priority - b.priority);
        const index = issues.indexOf(issue);
        if (index < 0 || index >= issues.length - 1)
          return null;
        return issues[index + 1];
      })
    );
  }

}
