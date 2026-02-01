import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'issueIcon',
  standalone: true
})
export class IssueIconPipe implements PipeTransform {

  transform(priority: number): string {
    if (priority >= 1 && priority <= 2)
      return "@tui.shield-alert"
    if (priority >= 3 && priority <= 5)
      return "@tui.triangle-alert"
    return "@tui.circle-alert"
  }

}
