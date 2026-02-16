import {Moment} from 'moment';

export interface CommentEntity {
  id: string;
  userId: string;
  content: string | null;
  status: string | null;
  progressStatus: string | null;
  createdAt: Moment;
  updatedAt: Moment | null;
}
