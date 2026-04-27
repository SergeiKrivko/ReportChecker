import {Moment} from 'moment';

export interface LlmUsageIntervalEntity {
  date: Moment;
  totalRequests: number;
  totalTokens: number;
}
