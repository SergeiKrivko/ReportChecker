import { DOCUMENT } from '@angular/common';
import { inject, InjectionToken } from '@angular/core';
import { MemoryStorage, OAuthStorage } from 'angular-oauth2-oidc';

const getLocalStorage = (): OAuthStorage => {
  const storage = inject(DOCUMENT).defaultView?.localStorage;

  if (!storage) {
    return new MemoryStorage();
  }

  try {
    const probeKey = '__oauth_storage_probe__';
    storage.setItem(probeKey, probeKey);
    storage.removeItem(probeKey);

    return storage;
  } catch {
    return new MemoryStorage();
  }
};

export const AUTH_STORAGE = new InjectionToken<OAuthStorage>('AUTH_STORAGE', {
  factory: getLocalStorage,
});
