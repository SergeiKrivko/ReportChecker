import moment from 'moment';
import {Comment, IPatch, IPatchLine, Issue, PatchStatus} from '../services/api-client';
import {IssueEntity} from '../entities/issue-entity';
import {CommentEntity} from '../entities/comment-entity';
import {PatchEntity, PatchLineEntity, PatchStatusEntity} from '../entities/patch-entity';

export const issueToEntity = (issue: Issue): IssueEntity => ({
  id: issue.id ?? "",
  title: issue.title ?? "",
  status: issue.status ?? "Open",
  priority: issue.priority ?? 10,
  comments: issue.comments?.map(commentToEntity) ?? [],
  chapter: issue.chapter ?? null,
  unreadCount: issue.comments?.filter(e => e.isRead === false).length ?? 0,
});

export const commentToEntity = (comment: Comment): CommentEntity => ({
  id: comment.id ?? "",
  userId: comment.userId ?? "",
  content: comment.content ?? null,
  status: comment.status ?? null,
  progressStatus: comment.progressStatus ?? null,
  isRead: comment.isRead ?? null,
  patch: comment.patch ? patchToEntity(comment.patch) : undefined,
  createdAt: comment.createdAt ?? moment(),
  updatedAt: comment.modifiedAt ?? null,
});

export const patchToEntity = (dto: IPatch): PatchEntity => ({
  id: dto.id,
  commentId: dto.commentId,
  status: statusMap[dto.status],
  lines: dto.lines?.map(patchLineToEntity) ?? [],
});

export const patchLineToEntity = (dto: IPatchLine): PatchLineEntity => ({
  number: dto.number,
  content: dto.content,
  previousContent: dto.previousContent,
  type: dto.type ?? "Unknown",
});

const statusMap: Record<PatchStatus, PatchStatusEntity> = {
  [PatchStatus.Pending]: PatchStatusEntity.Pending,
  [PatchStatus.InProgress]: PatchStatusEntity.InProgress,
  [PatchStatus.Completed]: PatchStatusEntity.Completed,
  [PatchStatus.Accepted]: PatchStatusEntity.Accepted,
  [PatchStatus.Rejected]: PatchStatusEntity.Rejected,
  [PatchStatus.Applied]: PatchStatusEntity.Applied,
  [PatchStatus.Failed]: PatchStatusEntity.Failed,
}
