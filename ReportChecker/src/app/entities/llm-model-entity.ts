import {LlmReasoningEffort} from '../services/api-client';

export interface LlmModelEntity {
  id: string;
  displayName?: string;
  inputCoefficient?: number;
  outputCoefficient?: number;
  defaultReasoningLevel?: LlmReasoningEffort;
  isRecommended?: boolean;
}
