export interface PatchEntity {
  id: string;
  commentId: string;
  status: PatchStatusEntity;
  lines: PatchLineEntity[];
}

export interface PatchLineEntity {
  number: number;
  content?: string;
  type: string;
}

export enum PatchStatusEntity {
    Pending = "Pending",
    InProgress = "InProgress",
    Completed = "Completed",
    Failed = "Failed",
    Accepted = "Accepted",
    Rejected = "Rejected",
    Applied = "Applied",
}
