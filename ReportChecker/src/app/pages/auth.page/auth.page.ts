import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {AuthService} from '../../auth/auth.service';
import {AsyncPipe} from '@angular/common';
import {TuiCard} from '@taiga-ui/layout';
import {TuiButton} from '@taiga-ui/core';
import {AccountInfoEntity} from '../../entities/user-info-entity';
import {map, Observable, switchMap} from 'rxjs';
import {TuiAvatar, TuiBadge, TuiInputRange} from '@taiga-ui/kit';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {SubscriptionLimit} from '../../components/subscription-limit/subscription-limit';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {AuthClient} from '../../auth/auth.client';
import {SubscriptionPlanByIdPipe} from '../../pipes/subscription-plan-by-id-pipe';
import {DateFromNowPipe} from '../../pipes/date-from-now-pipe';
import {Moment} from 'moment';

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
    TuiAvatar,
    TuiInputRange,
    SubscriptionLimit,
    RouterLink,
    SubscriptionPlanByIdPipe,
    TuiBadge,
    DateFromNowPipe
  ],
  templateUrl: './auth.page.html',
  styleUrl: './auth.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPage {
  private readonly subscriptionsService = inject(SubscriptionsService);
  private readonly authService = inject(AuthService);
  private readonly authClient = inject(AuthClient);
  private readonly activatedRoute = inject(ActivatedRoute);

  private readonly authProviders$$: AuthProvider[] = [
    {key: "password", name: "Логин и пароль"},
    {key: "yandex", name: "Яндекс"},
    {key: "google", name: "Google"},
    {key: "github", name: "GitHub"},
    {key: "gitlab", name: "GitLab"},
    {key: "microsoft", name: "Microsoft"},
  ];

  protected readonly currentSubscription$ = this.subscriptionsService.current$;
  protected readonly currentEndsAt$: Observable<Moment | undefined> = this.currentSubscription$.pipe(
    map(current => {
      if (!current?.active)
        return undefined;
      let endsAt = current?.active?.endsAt;
      for (const futureSubscription of current.future) {
        if (futureSubscription.planId != current.active.planId)
          break;
        endsAt = futureSubscription.endsAt;
      }
      return endsAt;
    })
  );

  protected authProviders$: Observable<AuthProvider[]> = this.authClient.userInfo$.pipe(
    map(userInfo => this.authProviders$$.map(provider => {
      const accountInfo = userInfo?.accounts.find(e => e.provider == provider.key);
      return {key: provider.key, name: provider.name, userInfo: accountInfo};
    })),
  );
  protected readonly isAdmin$: Observable<boolean> = this.authClient.userInfo$.pipe(
    map(userInfo => userInfo?.id === 'b13fa26b-0a30-4558-a2cd-da2d68022bab')
  );

  protected readonly isAuthenticated = this.authService.isAuthenticated;

  protected logIn(provider: string) {
    if (this.authService.isAuthenticated()) {
      this.authClient.getLinkCode().pipe(
        switchMap(linkCode => this.authService.login$(provider, linkCode ?? undefined,
          this.activatedRoute.snapshot.queryParams["returnUrl"])),
      ).subscribe();
    } else
      this.authService.startOrContinueLogin$(provider, undefined, this.activatedRoute.snapshot.queryParams["returnUrl"]).subscribe();
  }

  protected logOut() {
    this.authService.logout();
  }
}
