import {Moment} from 'moment';

export interface InstructionEntity {
  id: string;
  reportId: string;
  content: string;
  createdAt: Moment | null;
  deletedAt: Moment | null;
}
