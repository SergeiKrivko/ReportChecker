import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {AuthService} from '../../services/auth-service';
import {from, NEVER, switchMap} from 'rxjs';

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
  private readonly detectorRef = inject(ChangeDetectorRef);

  ngOnInit() {
    this.route.queryParams.pipe(
      switchMap(queryParams => {
        const code: string = queryParams['code'];
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
