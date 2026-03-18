import {computed, inject, Injectable, signal} from '@angular/core';
import {OAuthService} from 'angular-oauth2-oidc';
import {
  catchError,
  EMPTY,
  finalize,
  from,
  map,
  Observable,
  of,
  shareReplay,
  switchMap,
} from 'rxjs';

import {OAUTH_CONFIG} from './auth.config';

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly oauthConfig = inject(OAUTH_CONFIG);
  private readonly oauthService = inject(OAuthService);
  private readonly accessTokenState = signal<string | null>(null);

  private refreshing$?: Observable<string | null>;
  private configured = false;

  readonly accessToken = computed(() => this.accessTokenState());
  readonly isAuthenticated = computed(() => this.accessTokenState() !== null);

  constructor() {
    this.oauthService.events.subscribe(() => {
      this.syncTokenState();
    });
  }

  initialize$(): Observable<string | null> {
    this.configure();

    return this.ensureDiscoveryDocumentLoaded$().pipe(
      switchMap(() => this.completeLoginFromRedirect$()),
      switchMap((token) => (token ? of(token) : this.refresh$())),
      catchError(() => of(this.syncTokenState())),
    );
  }

  startOrContinueLogin$(provider?: string, linkCode?: string): Observable<string | null> {
    this.configure();

    return this.ensureDiscoveryDocumentLoaded$().pipe(
      switchMap(() => this.completeLoginFromRedirect$()),
      switchMap((token) => (token ? of(token) : this.refreshOrLogin$(provider, linkCode))),
    );
  }

  token(): string | null {
    return this.syncTokenState();
  }

  logout(): void {
    this.oauthService.logOut();
    this.accessTokenState.set(null);
  }

  refreshOrLogin$(provider?: string, linkCode?: string): Observable<string | null> {
    const existingToken = this.token();

    if (existingToken) {
      return of(existingToken);
    }

    return this.refresh$().pipe(
      switchMap((newToken) => (newToken ? of(newToken) : this.login$(provider, linkCode))),
    );
  }

  login$(provider?: string, linkCode?: string): Observable<never> {
    this.oauthService.initLoginFlow('', linkCode ? {provider, link_code: linkCode} : {provider});

    return EMPTY;
  }

  private refresh$(): Observable<string | null> {
    if (!this.oauthService.getRefreshToken()) {
      return of(null);
    }

    if (this.refreshing$) {
      return this.refreshing$;
    }

    this.refreshing$ = from(this.oauthService.refreshToken()).pipe(
      switchMap(() => of(this.syncTokenState())),
      catchError(() => of(null)),
      finalize(() => {
        this.refreshing$ = undefined;
      }),
      shareReplay({bufferSize: 1, refCount: true}),
    );

    return this.refreshing$;
  }

  private ensureDiscoveryDocumentLoaded$(): Observable<void> {
    if (this.oauthService.discoveryDocumentLoaded) {
      return of(undefined);
    }

    return from(this.oauthService.loadDiscoveryDocument()).pipe(
      map(() => undefined),
    );
  }

  private completeLoginFromRedirect$(): Observable<string | null> {
    if (!this.hasLoginCallbackInUrl()) {
      return of(this.syncTokenState());
    }

    return from(this.oauthService.tryLoginCodeFlow()).pipe(
      switchMap(() => of(this.syncTokenState())),
      catchError(() => of(null)),
    );
  }

  private configure(): void {
    if (this.configured) {
      return;
    }

    this.oauthService.configure(this.oauthConfig);
    this.oauthService.setupAutomaticSilentRefresh();
    this.configured = true;
  }

  private syncTokenState(): string | null {
    const token = this.oauthService.hasValidAccessToken()
      ? this.oauthService.getAccessToken()
      : null;

    this.accessTokenState.set(token);

    return token;
  }

  private hasLoginCallbackInUrl(): boolean {
    if (!globalThis.location) {
      return false;
    }

    const query = new URLSearchParams(globalThis.location.search);

    return ['code', 'state', 'session_state', 'error'].some((key) =>
      query.has(key),
    );
  }
}
