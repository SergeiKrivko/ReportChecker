import { Pipe, PipeTransform } from '@angular/core';
import {TuiAppearanceOptions} from '@taiga-ui/core';

@Pipe({
  name: 'issueAppearance',
  standalone: true
})
export class IssueAppearancePipe implements PipeTransform {

  transform(priority: number): TuiAppearanceOptions["appearance"] {
    if (priority >= 1 && priority <= 2)
      return "negative"
    if (priority >= 3 && priority <= 5)
      return "warning"
    return "positive"
  }

}
