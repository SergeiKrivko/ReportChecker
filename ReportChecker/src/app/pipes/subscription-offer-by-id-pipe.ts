import {inject, Pipe, PipeTransform} from '@angular/core';
import {SubscriptionsService} from '../services/subscriptions.service';
import {map, Observable, of} from 'rxjs';
import {SubscriptionOfferEntity, SubscriptionPlanEntity} from '../entities/subscription-plan-entity';

@Pipe({
  name: 'subscriptionOfferById',
})
export class SubscriptionOfferByIdPipe implements PipeTransform {
  private readonly subscriptionService = inject(SubscriptionsService);

  transform(value?: string | null | undefined): Observable<SubscriptionOfferEntity | undefined> {
    if (!value)
      return of(undefined);
    return this.subscriptionService.plans$.pipe(
      map(plans => {
        for (const plan of plans) {
          const offer = plan.offers.find(e => e.id == value);
          if (offer)
            return offer;
        }
        return undefined;
      }),
    );
  }
}
