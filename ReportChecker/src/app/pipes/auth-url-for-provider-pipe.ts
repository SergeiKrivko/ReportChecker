import {Inject, Pipe, PipeTransform} from '@angular/core';
import {API_BASE_URL} from '../services/api-client';

@Pipe({
  name: 'authUrlForProvider',
  standalone: true
})
export class AuthUrlForProviderPipe implements PipeTransform {
  constructor(@Inject(API_BASE_URL) protected readonly apiBaseUrl: string) {
  }

  transform(value: string): unknown {
    return `${this.apiBaseUrl}/api/v1/auth/${value}?redirectUrl=${window.location.origin}/auth/callback`;
  }

}
