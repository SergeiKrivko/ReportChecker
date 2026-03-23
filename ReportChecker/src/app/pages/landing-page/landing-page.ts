import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {Router, RouterLink} from '@angular/router';
import {TuiButton, TuiScrollbar} from '@taiga-ui/core';
import {TuiCard} from '@taiga-ui/layout';
import {TuiAvatar} from '@taiga-ui/kit';
import {AuthService} from '../../auth/auth.service';
import {takeUntilDestroyed, toObservable} from '@angular/core/rxjs-interop';
import {from, NEVER, switchMap} from 'rxjs';

@Component({
  selector: 'app-landing-page',
  imports: [
    RouterLink,
    TuiButton,
    TuiCard,
    TuiAvatar,
    TuiScrollbar
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
