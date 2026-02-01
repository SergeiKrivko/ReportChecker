import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {AuthService} from '../../services/auth-service';
import {AsyncPipe} from '@angular/common';
import {TuiCard} from '@taiga-ui/layout';
import {TuiButton} from '@taiga-ui/core';

@Component({
  selector: 'app-auth.page',
  imports: [
    AsyncPipe,
    TuiCard,
    TuiButton
  ],
  templateUrl: './auth.page.html',
  styleUrl: './auth.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly authService: AuthService = inject(AuthService);

  protected readonly authProviders$ = this.authService.providers$;
}
