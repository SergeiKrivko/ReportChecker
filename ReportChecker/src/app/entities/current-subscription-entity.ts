import {Moment} from 'moment';

export interface LimitEntity<T> {
  current: T;
  maximum: T;
}

export interface CurrentSubscriptionEntity {
  active?: SubscriptionEntity | null;
  future: SubscriptionEntity[];
  reportsLimit: LimitEntity<number>;
  tokensLimit: LimitEntity<number>;
}

export interface SubscriptionEntity {
  id: string;
  userId: string;
  planId: string;
  createdAt: Moment;
  confirmedAt?: Moment;
  deletedAt?: Moment;
  startsAt: Moment;
  endsAt: Moment;
}
