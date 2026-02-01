import { Pipe, PipeTransform } from '@angular/core';
import {CommentEntity} from '../entities/comment-entity';

@Pipe({
  name: 'orderByCreatedAt',
  standalone: true
})
export class OrderByCreatedAtPipe implements PipeTransform {

  transform(value: CommentEntity[]): CommentEntity[] {
    return value.sort((a, b) => a.createdAt.diff(b.createdAt));
  }

}
