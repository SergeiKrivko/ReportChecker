import {Component, inject, OnInit, signal} from '@angular/core';
import {Router, RouterOutlet} from '@angular/router';
import {TuiRoot} from '@taiga-ui/core';
import {AuthService} from './services/auth-service';
import {combineLatest, from, NEVER, switchMap} from 'rxjs';
import {ReportsService} from './services/reports.service';
import {IssuesService} from './services/issues.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TuiRoot],
  templateUrl: './app.html',
  standalone: true,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);
  private readonly router = inject(Router);
  protected readonly title = signal('ReportChecker');

  ngOnInit() {
    this.authService.loadProviders().pipe(
      switchMap(authorized => {
        if (authorized)
          return combineLatest([
            this.reportsService.loadReports(),
            this.issuesService.loadIssuesOnReportChanged$,
          ]);
        return from(this.router.navigate(["auth"]));
      })
    ).subscribe();
  }
}
