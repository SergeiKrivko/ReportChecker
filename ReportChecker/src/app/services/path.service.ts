import {Injectable} from '@angular/core';
import {PathItemEntity} from '../entities/path-item-entity';
import {patchState, signalState} from '@ngrx/signals';
import {Observable} from 'rxjs';
import {toObservable} from '@angular/core/rxjs-interop';

interface PathStore {
  items: Observable<PathItemEntity>[];
}

@Injectable({
  providedIn: 'root',
})
export class PathService {
  private readonly store$$ = signalState<PathStore>({
    items: [],
  });

  readonly items$ = toObservable(this.store$$.items);

  add(item: Observable<PathItemEntity>, level: number) {
    const items = this.store$$.items().slice(0, level);
    items.push(item);
    patchState(this.store$$, {
      items: items,
    });
  }

  clear(level: number = 0): void {
    patchState(this.store$$, {items: this.store$$.items().slice(0, level)});
  }
}
