import {Component, input} from '@angular/core';
import {TuiChip, TuiInputRange} from '@taiga-ui/kit';
import {LimitEntity} from '../../entities/limits-entity';
import {map, Observable} from 'rxjs';
import {TuiAppearanceOptions} from '@taiga-ui/core';
import {toObservable} from '@angular/core/rxjs-interop';
import {AsyncPipe} from '@angular/common';

@Component({
  selector: 'app-subscription-limit',
  imports: [
    TuiInputRange,
    TuiChip,
    AsyncPipe
  ],
  templateUrl: './subscription-limit.html',
  styleUrl: './subscription-limit.scss',
})
export class SubscriptionLimit {
  name = input<string>();
  limit = input.required<LimitEntity<number>>();

  protected readonly appearance$: Observable<TuiAppearanceOptions["appearance"]> = toObservable(this.limit).pipe(
    map(limit => {
      if (limit.current >= limit.maximum)
        return 'negative';
      if (limit.current >= limit.maximum * 0.75)
        return 'warning';
      return 'positive';
    })
  );
}
