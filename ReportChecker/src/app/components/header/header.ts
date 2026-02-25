import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {AuthService} from '../../services/auth-service';
import {RouterLink} from '@angular/router';
import {map} from 'rxjs';
import {TuiAvatar} from '@taiga-ui/kit';
import {TuiButton, TuiIcon} from '@taiga-ui/core';
import {TuiLet} from '@taiga-ui/cdk';
import {AsyncPipe} from '@angular/common';

@Component({
  selector: 'app-header',
  imports: [
    TuiAvatar,
    RouterLink,
    TuiButton,
    TuiLet,
    AsyncPipe,
    TuiIcon
  ],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  private readonly authService = inject(AuthService);

  showTitle = input<boolean>();

  protected readonly userInfo$ = this.authService.userInfo$.pipe(
    map(info => info?.accounts[0]),
  );
}
