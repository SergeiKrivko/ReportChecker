import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'avatarByUserId',
  standalone: true
})
export class AvatarByUserIdPipe implements PipeTransform {

  transform(userId: string): string {
    if (userId == "00000000-0000-0000-0000-000000000000")
      return "@tui.bot"
    return "@tui.user"
  }

}
