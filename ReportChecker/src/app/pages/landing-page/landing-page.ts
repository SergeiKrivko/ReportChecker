import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Router, RouterLink} from '@angular/router';
import {TuiAppearance, TuiButton, TuiLink} from '@taiga-ui/core';
import {TuiCard} from '@taiga-ui/layout';
import {TuiAvatar, TuiChip} from '@taiga-ui/kit';
import {AuthService} from '../../auth/auth.service';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {first, from, NEVER, switchMap} from 'rxjs';

@Component({
  selector: 'app-landing-page',
  imports: [
    RouterLink,
    TuiButton,
    TuiCard,
    TuiAppearance,
    TuiChip,
    TuiAvatar,
    TuiLink
  ],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.scss',
})
export class LandingPage implements OnInit {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly isAuthenticated$ = toObservable(this.authService.isAuthenticated);

  ngOnInit() {
    this.isAuthenticated$.pipe(
      switchMap(isAuthenticated => {
        if (isAuthenticated) {
          return from(this.router.navigateByUrl('reports'));
        }
        return NEVER;
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }
}
