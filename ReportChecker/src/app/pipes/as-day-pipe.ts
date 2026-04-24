import { Pipe, PipeTransform } from '@angular/core';
import {Moment} from 'moment';

@Pipe({
  name: 'asDay',
})
export class AsDayPipe implements PipeTransform {

  transform(value?: Moment): string | undefined {
    if (!value)
      return undefined;
    return value.format('DD.MM.YYYY');
  }

}
