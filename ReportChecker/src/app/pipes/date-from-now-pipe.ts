import { Pipe, PipeTransform } from '@angular/core';
import {Moment} from 'moment';
import 'moment/locale/ru.js'

@Pipe({
  name: 'dateFromNow',
})
export class DateFromNowPipe implements PipeTransform {

  transform(value?: Moment | null | undefined): string {
    if (!value)
      return '?';
    return value.fromNow(true);
  }

}
