import {Component, inject, OnInit} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {TuiRoot} from '@taiga-ui/core';
import {AuthService} from './services/auth-service';
import {SubscriptionsService} from './services/subscriptions.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TuiRoot],
  templateUrl: './app.html',
  standalone: true,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly subscriptionsService = inject(SubscriptionsService);

  ngOnInit() {
    this.authService.loadAuthorization().subscribe();
    this.subscriptionsService.loadLimits$.subscribe();
  }
}
