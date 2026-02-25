import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'isBot',
  standalone: true
})
export class IsBotPipe implements PipeTransform {

  transform(userId: string): boolean {
    return userId === "00000000-0000-0000-0000-000000000000";
  }

}
