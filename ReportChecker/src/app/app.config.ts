import {APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners} from '@angular/core';
import {provideRouter} from '@angular/router';

import {routes} from './app.routes';
import {NG_EVENT_PLUGINS} from '@taiga-ui/event-plugins';
import {provideAnimations} from '@angular/platform-browser/animations';
import {API_BASE_URL} from './services/api-client';
import {OAuthStorage, provideOAuthClient} from 'angular-oauth2-oidc';
import {AUTH_STORAGE} from './auth/auth.storage';
import {AuthService} from './auth/auth.service';
import {firstValueFrom} from 'rxjs';

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: (authService: AuthService) => () =>
        firstValueFrom(authService.initialize$()),
      deps: [AuthService],
    },
    // { provide: API_BASE_URL, useValue: "http://localhost:5000" },
    { provide: API_BASE_URL, useValue: "https://reportchecker.nachert.art" },
    provideAnimations(),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    NG_EVENT_PLUGINS,
    provideOAuthClient(),
    { provide: OAuthStorage, useExisting: AUTH_STORAGE },
  ]
};
