import {Component, DestroyRef, inject, input, OnInit} from '@angular/core';
import {
  ApiClient,
  CreateSubscriptionOfferSchema,
  CreateSubscriptionPlanSchema,
  SubscriptionPlan
} from '../../services/api-client';
import {FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {debounceTime, switchMap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiAppearance, TuiButton, TuiLabel, TuiTextfieldComponent, TuiTextfieldDirective} from '@taiga-ui/core';
import {TuiCardLarge} from '@taiga-ui/layout';
import {TuiInputNumberDirective, TuiTextarea} from '@taiga-ui/kit';
import {AdminSubscriptionOffer} from '../admin-subscription-offer/admin-subscription-offer';

@Component({
  selector: 'app-admin-subscription-plan',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    TuiAppearance,
    TuiCardLarge,
    TuiInputNumberDirective,
    TuiLabel,
    TuiTextfieldComponent,
    TuiTextfieldDirective,
    TuiTextarea,
    AdminSubscriptionOffer,
    TuiButton
  ],
  templateUrl: './admin-subscription-plan.html',
  styleUrl: './admin-subscription-plan.scss',
})
export class AdminSubscriptionPlan implements OnInit {
  private readonly apiClient = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);

  readonly subscriptionPlan = input.required<SubscriptionPlan>();

  protected readonly control = new FormGroup({
    name: new FormControl<string>(""),
    description: new FormControl<string>(""),
    tokensLimit: new FormControl<number>(1),
    reportsLimit: new FormControl<number>(1),
  });

  ngOnInit() {
    const plan = this.subscriptionPlan();
    this.control.setValue({
      name: plan.name ?? "",
      description: plan.description ?? null,
      tokensLimit: plan.tokensLimit ?? 1,
      reportsLimit: plan.reportsLimit ?? 1,
    });

    this.control.valueChanges.pipe(
      debounceTime(1000),
      switchMap(value => this.apiClient.plansPUT(this.subscriptionPlan().id, CreateSubscriptionPlanSchema.fromJS(value))),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected addOffer() {
    this.apiClient.offersPOST(this.subscriptionPlan().id, CreateSubscriptionOfferSchema.fromJS({
      months: 1,
      price: 0,
    })).pipe(
      // switchMap(() => this.loadPlans()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected deleteOffer(offerId: string) {
    this.apiClient.offersDELETE(this.subscriptionPlan().id, offerId).pipe(
      // switchMap(() => this.loadPlans()),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
