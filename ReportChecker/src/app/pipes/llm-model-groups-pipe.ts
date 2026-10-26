import {Pipe, PipeTransform} from '@angular/core';
import {LlmModelEntity} from '../entities/llm-model-entity';

export const LLM_MODEL_GROUP_LABELS: readonly string[] = ['Рекомендуемые', 'Остальные'];

@Pipe({
  name: 'llmModelGroups',
})
export class LlmModelGroupsPipe implements PipeTransform {
  transform(models?: readonly LlmModelEntity[] | null): readonly (readonly LlmModelEntity[])[] {
    const all = models ?? [];
    return [
      all.filter(model => model.isRecommended),
      all.filter(model => !model.isRecommended),
    ];
  }
}
