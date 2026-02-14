import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {AuthService} from '../../services/auth-service';
import {combineLatest, first, from, NEVER, switchMap} from 'rxjs';

@Component({
  selector: 'app-auth-redirect.page',
  imports: [],
  templateUrl: './auth-redirect.page.html',
  styleUrl: './auth-redirect.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRedirectPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService: AuthService = inject(AuthService);

  ngOnInit() {
    combineLatest([
      this.authService.isAuthorized$,
      this.route.queryParams
    ]).pipe(
      first(),
      switchMap(([isAuthorized, queryParams]) => {
        const code: string = queryParams['code'];
        if (isAuthorized)
          return this.authService.linkAccount(code)
        return this.authService.getToken(code);
      }),
      switchMap(success => {
        if (success)
          return from(this.router.navigate(['/']));
        return NEVER;
      }),
    ).subscribe();
  }
}
