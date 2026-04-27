import {IReportSourceUnion} from '../services/api-client';
import {Moment} from 'moment';

export interface ReportEntity {
  id: string;
  name: string;
  sourceProvider: string;
  format: string;
  llmModelId?: string;

  source?: IReportSourceUnion,
  issueCount?: { [key: string]: number; },
  updatedAt?: Moment;
}
