import {Component, DestroyRef, inject, input} from '@angular/core';
import {FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {TuiLabel, TuiTextfieldComponent} from '@taiga-ui/core';
import {TuiInputNumberDirective} from '@taiga-ui/kit';
import {
  ApiClient,
  CreateSubscriptionOfferSchema,
  SubscriptionOffer, SubscriptionPlan
} from '../../services/api-client';
import {debounceTime, switchMap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-admin-subscription-offer',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    TuiInputNumberDirective,
    TuiLabel,
    TuiTextfieldComponent,
  ],
  templateUrl: './admin-subscription-offer.html',
  styleUrl: './admin-subscription-offer.scss',
})
export class AdminSubscriptionOffer {
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);

  readonly subscriptionPlan = input.required<SubscriptionPlan>();
  readonly subscriptionOffer = input.required<SubscriptionOffer>();

  protected readonly control = new FormGroup({
    months: new FormControl<number>(1),
    price: new FormControl<number>(1),
  });

  ngOnInit() {
    const plan = this.subscriptionOffer();
    this.control.setValue({
      months: plan.months ?? 1,
      price: plan.price ?? 0,
    });

    this.control.valueChanges.pipe(
      debounceTime(1000),
      switchMap(value => this.apiClient.offersPUT(this.subscriptionPlan().id, this.subscriptionOffer().id, CreateSubscriptionOfferSchema.fromJS(value))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
