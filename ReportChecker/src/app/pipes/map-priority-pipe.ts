import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'mapPriority',
})
export class MapPriorityPipe implements PipeTransform {

  transform(value?: { [key: string]: number; }, priority: number = 0): number {
    if (value && priority === 0)
      return (value[1] || 0) + (value[2] || 0) + (value[3] || 0) + (value[4] || 0) + (value[5] || 0) + (value[6] || 0) + (value[7] || 0) + (value[8] || 0) + (value[9] || 0) + (value[10] || 0);
    if (!value || !priority)
      return 0;
    if (priority == 1)
      return (value[1] || 0) + (value[2] || 0);
    if (priority == 2)
      return (value[3] || 0) + (value[4] || 0) + (value[5] || 0);
    return (value[6] || 0) + (value[7] || 0) + (value[8] || 0) + (value[9] || 0) + (value[10] || 0);
  }

}
