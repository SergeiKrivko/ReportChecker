import {Injectable} from "@angular/core";

@Injectable()
export class ApiClientBase {
  private authorization: string | undefined;

  setAuthorization(auth: string | undefined) {
    this.authorization = auth;
  }

  transformOptions(options: any): Promise<any> {
    if (this.authorization) {
      options.headers = options.headers.set("Authorization", this.authorization);
    }
    return Promise.resolve(options);
  }
}
