import {Observable} from 'rxjs';

export interface IAuthProvider {
  key: string;
  displayName: string;
  url: string;

  load(): Observable<boolean>;
  authorize(credentials: object): Observable<boolean>;
  getIdToken(): Observable<string | null>;
}
