import {inject, Injectable} from "@angular/core";
import {AuthService} from '../auth/auth.service';
import {HttpErrorResponse, HttpResponseBase} from '@angular/common/http';
import {from, map, Observable, switchMap, tap} from 'rxjs';
import {TuiAlertService} from '@taiga-ui/core';

interface ProblemDetailsEntity {
  title: string;
  status: number;
  detail: string;
}

@Injectable()
export class ApiClientBase {
  private readonly authService = inject(AuthService);

  private readonly alerts = inject(TuiAlertService);

  transformOptions(options: any): Promise<any> {
    options.headers = options.headers.set("Authorization", "Bearer " + this.authService.accessToken());
    return Promise.resolve(options);
  }

  transformResult(url: string, response: HttpResponseBase, defaultProcessor: (response: HttpResponseBase) => Observable<any>): Observable<any> {
    if (!response.ok) {
      const errorBlob = (response as HttpErrorResponse).error as Blob;
      return from(errorBlob.text()).pipe(
        map((data) => JSON.parse(data) as ProblemDetailsEntity),
        tap(error => {
          console.log(error);
          this.alerts
            .open(error.title, {label: "Ошибка", appearance: "negative"})
            .subscribe();
        }),
        switchMap(() => defaultProcessor(response)),
      );
    }
    return defaultProcessor(response);
  }
}
