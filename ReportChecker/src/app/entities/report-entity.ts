import {IReportSourceUnion, LlmReasoningEffort} from '../services/api-client';
import {Moment} from 'moment';

export interface ReportEntity {
  id: string;
  name: string;
  sourceProvider: string;
  format: string;
  llmModelId?: string;
  imageProcessingMode: ImageProcessingModeEntity;
  reasoningLevel: LlmReasoningEffort;

  source?: IReportSourceUnion,
  issueCount?: { [key: string]: number; },
  updatedAt?: Moment;
}

export enum ImageProcessingModeEntity {
    Disable = "Disable",
    Auto = "Auto",
    LowDetail = "LowDetail",
    HighDetail = "HighDetail",
}
