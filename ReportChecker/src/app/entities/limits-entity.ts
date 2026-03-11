export interface LimitEntity<T> {
  current: T;
  maximum: T;
}

export interface LimitsEntity {
  reports: LimitEntity<number>;
  checks: LimitEntity<number>;
  comments: LimitEntity<number>;
}
