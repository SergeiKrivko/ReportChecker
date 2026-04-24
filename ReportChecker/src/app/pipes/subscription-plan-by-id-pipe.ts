import {inject, Pipe, PipeTransform} from '@angular/core';
import {SubscriptionPlanEntity} from '../entities/subscription-plan-entity';
import {SubscriptionsService} from '../services/subscriptions.service';
import {map, Observable, of} from 'rxjs';

@Pipe({
  name: 'subscriptionPlanById',
})
export class SubscriptionPlanByIdPipe implements PipeTransform {
  private readonly subscriptionService = inject(SubscriptionsService);

  transform(value?: string): Observable<SubscriptionPlanEntity | undefined> {
    if (!value)
      return of(undefined);
    return this.subscriptionService.plans$.pipe(
      map(plans => plans.find(e => e.id === value)),
    );
  }

}
