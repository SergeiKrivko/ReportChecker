import { HttpErrorResponse } from '@angular/common/http';

export const isAuthError = (error: HttpErrorResponse): boolean =>
  error.status === 401 || error.status === 403;
