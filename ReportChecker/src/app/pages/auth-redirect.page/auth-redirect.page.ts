import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {AuthService} from '../../services/auth-service';
import {combineLatest, from, NEVER, of, switchMap, take, tap} from 'rxjs';
import {IAuthProvider} from '../../services/providers/auth-provider';

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

  protected provider: IAuthProvider | undefined;

  ngOnInit() {
    combineLatest([this.route.params, this.route.queryParams]).pipe(
      tap(([pathParams, queryParams]) => {
        this.provider = this.authService.providers$.find(e => e.key == pathParams["provider"]);
        this.detectorRef.detectChanges();
      }),
      switchMap(([_, queryParams]) => {
          if (this.provider)
            return this.authService.authorize(this.provider, queryParams);
          return of(false);
        }
      ),
      switchMap(success => {
        if (success)
          return from(this.router.navigate(['/']));
        return NEVER;
      }),
    ).subscribe();
  }
}
