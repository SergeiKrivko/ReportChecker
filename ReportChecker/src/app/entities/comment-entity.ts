import {Moment} from 'moment';
import {PatchEntity} from './patch-entity';

export interface CommentEntity {
  id: string;
  userId: string;
  content: string | null;
  status: string | null;
  progressStatus: string | null;
  isRead: boolean | null;
  patch?: PatchEntity;
  createdAt: Moment;
  updatedAt: Moment | null;
}
