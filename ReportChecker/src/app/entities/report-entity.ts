export interface ReportEntity {
  id: string;
  name: string;
  sourceProvider: string;
  format: string;
  llmModelId?: string;
}
