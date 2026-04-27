import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {map, Observable} from 'rxjs';
import {TuiAvatar, TuiBadge, TuiBreadcrumbs} from '@taiga-ui/kit';
import {
  TuiAppearance,
  TuiButton,
  TuiDataList,
  TuiDropdown,
  TuiIcon,
  TuiLabel,
  TuiLink,
  TuiScrollbar
} from '@taiga-ui/core';
import {TuiItem} from '@taiga-ui/cdk';
import {AsyncPipe} from '@angular/common';
import {AuthClient} from '../../auth/auth.client';
import {PathService} from '../../services/path.service';
import {SubscriptionLimit} from '../subscription-limit/subscription-limit';
import {SubscriptionsService} from '../../services/subscriptions.service';
import {Moment} from 'moment/moment';
import {SubscriptionPlanByIdPipe} from '../../pipes/subscription-plan-by-id-pipe';
import {AsDayPipe} from '../../pipes/as-day-pipe';
import moment from 'moment';
import {AuthService} from '../../auth/auth.service';

@Component({
  selector: 'app-header',
  imports: [
    TuiAvatar,
    RouterLink,
    TuiButton,
    AsyncPipe,
    TuiBreadcrumbs,
    TuiLink,
    TuiItem,
    TuiScrollbar,
    TuiIcon,
    TuiDataList,
    TuiAppearance,
    TuiDropdown,
    SubscriptionLimit,
    SubscriptionPlanByIdPipe,
    TuiBadge,
    TuiLabel,
    AsDayPipe,
  ],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  private readonly authClient = inject(AuthClient);
  private readonly authService = inject(AuthService);
  private readonly pathService = inject(PathService);
  private readonly subscriptionsService = inject(SubscriptionsService);

  protected items$ = this.pathService.items$;
  protected showTitle$ = this.pathService.items$.pipe(
    map(items => items.length == 0),
  );

  protected readonly userInfo$ = this.authClient.userInfo$.pipe(
    map(info => info?.accounts[0]),
  );

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
  protected readonly resetLimitsAt$ = this.currentSubscription$.pipe(
    map(current => {
      let day = current?.active?.startsAt;
      if (!day)
        return undefined;
      const now = moment();
      while (day < now) {
        day.add(30, 'days');
      }
      return day;
    })
  );

  protected readonly isAdmin$: Observable<boolean> = this.authClient.userInfo$.pipe(
    map(userInfo => userInfo?.id === 'b13fa26b-0a30-4558-a2cd-da2d68022bab')
  );

  protected readonly routerLink$ = this.userInfo$.pipe(
    map(userInfo => userInfo ? '/reports' : '/'),
  );

  protected logOut() {
    this.authService.logout();
  }
}
