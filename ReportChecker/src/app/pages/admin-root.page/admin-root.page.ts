import {Component, inject, OnDestroy, OnInit} from '@angular/core';
import {TuiSegmented} from '@taiga-ui/kit';
import {RouterLink, RouterLinkActive, RouterOutlet} from '@angular/router';
import {PathService} from '../../services/path.service';
import {of} from 'rxjs';

@Component({
  selector: 'app-admin-root.page',
  imports: [
    TuiSegmented,
    RouterLinkActive,
    RouterLink,
    RouterOutlet
  ],
  templateUrl: './admin-root.page.html',
  styleUrl: './admin-root.page.scss',
})
export class AdminRootPage implements OnInit, OnDestroy {
  private readonly pathService = inject(PathService);

  ngOnInit() {
    this.pathService.add(of({name: "Админ", link: "/admin"}), 0);
  }

  ngOnDestroy() {
    this.pathService.clear(0);
  }
}
