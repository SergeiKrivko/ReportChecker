import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {map} from 'rxjs';
import {TuiAvatar, TuiBreadcrumbs} from '@taiga-ui/kit';
import {TuiButton, TuiLink} from '@taiga-ui/core';
import {TuiItem, TuiLet} from '@taiga-ui/cdk';
import {AsyncPipe} from '@angular/common';
import {AuthClient} from '../../auth/auth.client';
import {PathService} from '../../services/path.service';

@Component({
  selector: 'app-header',
  imports: [
    TuiAvatar,
    RouterLink,
    TuiButton,
    TuiLet,
    AsyncPipe,
    TuiBreadcrumbs,
    TuiLink,
    TuiItem,
  ],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  private readonly authClient = inject(AuthClient);
  private readonly pathService = inject(PathService);

  protected items$ = this.pathService.items$;
  protected showTitle$ = this.pathService.items$.pipe(
    map(items => items.length == 0),
  );

  protected readonly userInfo$ = this.authClient.userInfo$.pipe(
    map(info => info?.accounts[0]),
  );
}
