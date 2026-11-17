import {
  ImageProcessingMode,
  LlmReasoningEffort,
  Report,
  ReportAccessLevel
} from '../services/api-client';
import {ImageProcessingModeEntity, ReportEntity} from '../entities/report-entity';

export const reportToEntity = (report: Report): ReportEntity => ({
  id: report.id ?? "",
  ownerId: report.ownerId ?? "",
  accessLevel: report.accessLevel ?? ReportAccessLevel.Read,
  name: report.name ?? "",
  sourceProvider: report.sourceProvider ?? "File",
  format: report.format ?? "Latex",
  llmModelId: report.llmModelId,
  imageProcessingMode: imageProcessingModeMap[report.imageProcessingMode ?? ImageProcessingMode.Disable],
  reasoningLevel: report.reasoningLevel ?? LlmReasoningEffort.Default,
  source: report.source,
  issueCount: report.issueCount,
  updatedAt: report.updatedAt,
});

export const imageProcessingModeMap: Record<ImageProcessingMode, ImageProcessingModeEntity> = {
  [ImageProcessingMode.Auto]: ImageProcessingModeEntity.Auto,
  [ImageProcessingMode.Disable]: ImageProcessingModeEntity.Disable,
  [ImageProcessingMode.LowDetail]: ImageProcessingModeEntity.LowDetail,
  [ImageProcessingMode.HighDetail]: ImageProcessingModeEntity.HighDetail,
};
