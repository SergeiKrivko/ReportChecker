import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'iconBySource',
})
export class IconBySourcePipe implements PipeTransform {

  transform(value: string): string {
    return sourceIcons[value];
  }

}

const sourceIcons: Record<string, string> = {
  "GitHub": "github.svg",
  "File": "@tui.file",
  "Local": "@tui.computer",
}
