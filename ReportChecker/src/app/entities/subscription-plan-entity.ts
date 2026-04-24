import {Moment} from 'moment';

export interface SubscriptionPlanEntity {
  id: string;
  name?: string;
  description?: string;
  tokensLimit: number;
  reportsLimit: number;
  createdAt?: Moment;
  deletedAt?: Moment;
  offers: SubscriptionOfferEntity[];
}

export interface SubscriptionOfferEntity {
  id: string;
  planId: string;
  price: number;
  months: number;
  createdAt?: Moment;
  deletedAt?: Moment;
}
