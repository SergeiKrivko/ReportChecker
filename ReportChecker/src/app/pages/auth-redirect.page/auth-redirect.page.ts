import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {AuthService} from '../../auth/auth.service';
import {Router} from '@angular/router';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {NEVER, switchMap} from 'rxjs';

@Component({
  selector: 'app-auth-redirect.page',
  imports: [],
  templateUrl: './auth-redirect.page.html',
  styleUrl: './auth-redirect.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRedirectPage implements OnInit {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly isAuthenticated$ = toObservable(inject(AuthService).isAuthenticated);

  ngOnInit() {
    this.isAuthenticated$.pipe(
      switchMap(isAuthenticated => {
        if (isAuthenticated)
          return this.router.navigate(['/']);
        return NEVER;
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
