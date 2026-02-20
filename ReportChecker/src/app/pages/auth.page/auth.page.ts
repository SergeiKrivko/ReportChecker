import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {AuthService} from '../../services/auth-service';
import {AsyncPipe} from '@angular/common';
import {TuiCard} from '@taiga-ui/layout';
import {TuiButton} from '@taiga-ui/core';
import {AuthUrlForProviderPipe} from '../../pipes/auth-url-for-provider-pipe';
import {AccountInfoEntity} from '../../entities/user-info-entity';
import {map, Observable} from 'rxjs';
import {TuiAvatar, TuiInputRange} from '@taiga-ui/kit';

interface AuthProvider {
  key: string;
  name: string;
  userInfo?: AccountInfoEntity;
}

@Component({
  selector: 'app-auth.page',
  imports: [
    AsyncPipe,
    TuiCard,
    TuiButton,
    AuthUrlForProviderPipe,
    TuiAvatar,
    TuiInputRange
  ],
  templateUrl: './auth.page.html',
  styleUrl: './auth.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly authService: AuthService = inject(AuthService);

  private readonly authProviders$$: AuthProvider[] = [
    {key: "yandex", name: "Яндекс"},
    {key: "google", name: "Google"},
    {key: "github", name: "GitHub"},
  ];

  protected authProviders$: Observable<AuthProvider[]> = this.authService.userInfo$.pipe(
    map(userInfo => this.authProviders$$.map(provider => {
      const accountInfo = userInfo?.accounts.find(e => e.provider == provider.key);
      return {key: provider.key, name: provider.name, userInfo: accountInfo};
    })),
  );
}
