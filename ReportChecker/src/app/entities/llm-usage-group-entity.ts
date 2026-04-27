import {ReportEntity} from './report-entity';
import {LlmModelEntity} from './llm-model-entity';

export interface LlmUsageGroupEntity {
  report?: ReportEntity;
  model?: LlmModelEntity;
  totalTokens: number;
  totalRequests: number;
}
