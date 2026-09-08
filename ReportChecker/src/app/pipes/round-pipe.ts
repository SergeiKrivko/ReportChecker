import { Pipe, PipeTransform } from '@angular/core';
import {tuiRound} from '@taiga-ui/cdk';

@Pipe({
  name: 'round',
})
export class RoundPipe implements PipeTransform {

  transform(value?: number): number {
    return tuiRound(value ?? 0, 1);
  }

}
