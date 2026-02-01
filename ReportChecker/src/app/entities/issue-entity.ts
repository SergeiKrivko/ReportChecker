import {CommentEntity} from './comment-entity';

export interface IssueEntity {
  id: string;
  title: string;
  priority: number;
  status: string;
  chapter: string | null;
  comments: CommentEntity[];
}
