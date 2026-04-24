import { Pipe, PipeTransform } from '@angular/core';
import {LimitEntity} from '../entities/current-subscription-entity';

@Pipe({
  name: 'limitReached',
})
export class LimitReachedPipe implements PipeTransform {

  transform(value: LimitEntity<any> | undefined | null): boolean {
    if (!value)
      return true;
    return value.current >= value.maximum;
  }

}
