import {Component, inject, OnInit} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {TuiRoot} from '@taiga-ui/core';
import {AuthClient} from './auth/auth.client';
import {SubscriptionsService} from './services/subscriptions.service';
import {Header} from './components/header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TuiRoot, Header],
  templateUrl: './app.html',
  standalone: true,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly authClient = inject(AuthClient);
  private readonly subscriptionsService = inject(SubscriptionsService);

  ngOnInit() {
    this.authClient.loadUserInfo().subscribe();
    this.subscriptionsService.loadLimits$.subscribe();
  }
}
