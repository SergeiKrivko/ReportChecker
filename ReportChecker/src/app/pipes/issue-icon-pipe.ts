import {Pipe, PipeTransform} from '@angular/core';
import {IssueEntity} from '../entities/issue-entity';

@Pipe({
  name: 'issueIcon',
  standalone: true
})
export class IssueIconPipe implements PipeTransform {

  transform(issue: IssueEntity): string {
    if (issue.status == 'Open') {
      if (issue.priority >= 1 && issue.priority <= 2)
        return "@tui.shield-alert"
      if (issue.priority >= 3 && issue.priority <= 5)
        return "@tui.triangle-alert"
      return "@tui.circle-alert"
    }
    if (issue.status == 'Fixed')
      return "@tui.check"
    if (issue.status == 'Closed')
      return "@tui.x"
    return "@tui.circle-question-mark"
  }

}
