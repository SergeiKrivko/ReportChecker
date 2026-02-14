import {Component, inject, OnInit} from '@angular/core';
import {Router, RouterLink, RouterOutlet} from '@angular/router';
import {combineLatest, from, switchMap} from 'rxjs';
import {AuthService} from '../../services/auth-service';
import {ReportsService} from '../../services/reports.service';
import {IssuesService} from '../../services/issues.service';

@Component({
  selector: 'app-root.page',
  imports: [
    RouterOutlet,
    RouterLink
  ],
  templateUrl: './root.page.html',
  styleUrl: './root.page.scss',
  standalone: true
})
export class RootPage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);
  private readonly router = inject(Router);

  ngOnInit() {
    this.authService.isAuthorized$.pipe(
      switchMap(authorized => {
        if (authorized)
          return combineLatest([
            this.reportsService.loadReports(),
            this.issuesService.loadIssuesOnReportChanged$,
          ]);
        return from(this.router.navigate(["auth"]));
      }),
    ).subscribe();
  }
}
