import {ApplicationConfig, provideBrowserGlobalErrorListeners} from '@angular/core';
import {provideRouter} from '@angular/router';

import {routes} from './app.routes';
import {NG_EVENT_PLUGINS} from '@taiga-ui/event-plugins';
import {provideAnimations} from '@angular/platform-browser/animations';
import {API_BASE_URL} from './services/api-client';

export const appConfig: ApplicationConfig = {
  providers: [
    // { provide: API_BASE_URL, useValue: "http://localhost:5000" },
    { provide: API_BASE_URL, useValue: "https://reportchecker.nachert.art" },
    provideAnimations(),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    NG_EVENT_PLUGINS,
  ]
};
