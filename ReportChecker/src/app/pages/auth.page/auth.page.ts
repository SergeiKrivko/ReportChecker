import {ChangeDetectionStrategy, Component, DestroyRef, inject} from '@angular/core';
import {AuthService} from '../../services/auth-service';
import {AsyncPipe} from '@angular/common';
import {TuiCard} from '@taiga-ui/layout';
import {TuiButton, TuiScrollbar} from '@taiga-ui/core';
import {AuthUrlForProviderPipe} from '../../pipes/auth-url-for-provider-pipe';
import {AccountInfoEntity} from '../../entities/user-info-entity';
import {map, Observable} from 'rxjs';
import {TuiAvatar, TuiInputRange} from '@taiga-ui/kit';
import {Header} from '../../components/header/header';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {SubscriptionLimit} from '../../components/subscription-limit/subscription-limit';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {RouterLink} from '@angular/router';

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
    TuiInputRange,
    Header,
    TuiScrollbar,
    SubscriptionLimit,
    RouterLink
  ],
  templateUrl: './auth.page.html',
  styleUrl: './auth.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly authService: AuthService = inject(AuthService);
  private readonly subscriptionsService = inject(SubscriptionsService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly authProviders$$: AuthProvider[] = [
    {key: "yandex", name: "Яндекс"},
    {key: "google", name: "Google"},
    {key: "github", name: "GitHub"},
    {key: "gitlab", name: "GitLab"},
    {key: "microsoft", name: "Microsoft"},
  ];

  protected readonly limits$ = this.subscriptionsService.limits$;

  protected authProviders$: Observable<AuthProvider[]> = this.authService.userInfo$.pipe(
    map(userInfo => this.authProviders$$.map(provider => {
      const accountInfo = userInfo?.accounts.find(e => e.provider == provider.key);
      return {key: provider.key, name: provider.name, userInfo: accountInfo};
    })),
  );

  protected logOut() {
    this.authService.logOut().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
