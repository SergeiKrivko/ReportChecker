import { Pipe, PipeTransform } from '@angular/core';
import {TuiAppearanceOptions} from '@taiga-ui/core';
import {IssueEntity} from '../entities/issue-entity';

@Pipe({
  name: 'issueAppearance',
  standalone: true
})
export class IssueAppearancePipe implements PipeTransform {

  transform(issue: IssueEntity): TuiAppearanceOptions["appearance"] {
    if (issue.status == 'Open') {
      if (issue.priority >= 1 && issue.priority <= 2)
        return "negative";
      if (issue.priority >= 3 && issue.priority <= 5)
        return "warning";
      return "secondary";
    }
    if (issue.status == 'Fixed')
      return "positive";
    return "info";
  }

}
