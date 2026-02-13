import {ChangeDetectionStrategy, Component, Inject, inject} from '@angular/core';
import {AuthService} from '../../services/auth-service';
import {AsyncPipe} from '@angular/common';
import {TuiCard} from '@taiga-ui/layout';
import {TuiButton} from '@taiga-ui/core';
import {API_BASE_URL} from '../../services/api-client';
import {AuthUrlForProviderPipe} from '../../pipes/auth-url-for-provider-pipe';

@Component({
  selector: 'app-auth.page',
  imports: [
    AsyncPipe,
    TuiCard,
    TuiButton,
    AuthUrlForProviderPipe
  ],
  templateUrl: './auth.page.html',
  styleUrl: './auth.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly authService: AuthService = inject(AuthService);
}
