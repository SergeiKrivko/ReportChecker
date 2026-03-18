import {inject, Injectable} from "@angular/core";
import {AuthService} from '../auth/auth.service';

@Injectable()
export class ApiClientBase {
  private readonly authService = inject(AuthService);

  transformOptions(options: any): Promise<any> {
    options.headers = options.headers.set("Authorization", "Bearer " + this.authService.accessToken());
    return Promise.resolve(options);
  }
}
