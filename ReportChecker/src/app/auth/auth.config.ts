import { InjectionToken } from '@angular/core';
import { AuthConfig } from 'angular-oauth2-oidc';

export const OAUTH_CONFIG = new InjectionToken<AuthConfig>('OAUTH_CONFIG', {
  factory: () => {
    return {
      issuer: "https://auth.nachert.art/api/v1",
      clientId: "7c4a1272396979451d2a2d311f087050",
      dummyClientSecret: "64305c89201a3fce41287675d2c9b0a5",
      responseType: 'code',
      redirectUri: "http://localhost:4200/auth/callback",
      postLogoutRedirectUri: "http://localhost:4200/auth/callback",
      showDebugInformation: true,
    };
  },
});
