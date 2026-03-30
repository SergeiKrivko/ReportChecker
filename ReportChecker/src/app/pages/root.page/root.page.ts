import {Component, inject, OnInit} from '@angular/core';
import {Router, RouterOutlet} from '@angular/router';
import {combineLatest, from, switchMap} from 'rxjs';
import {ReportsService} from '../../services/reports.service';
import {IssuesService} from '../../services/issues.service';
import {AuthService} from '../../auth/auth.service';
import {toObservable} from '@angular/core/rxjs-interop';
import {InstructionService} from '../../services/instruction.service';

@Component({
  selector: 'app-root.page',
  imports: [
    RouterOutlet,
  ],
  templateUrl: './root.page.html',
  styleUrl: './root.page.scss',
  standalone: true
})
export class RootPage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly reportsService = inject(ReportsService);
  private readonly issuesService = inject(IssuesService);
  private readonly instructionService = inject(InstructionService);
  private readonly router = inject(Router);

  private readonly isAuthenticated$ = toObservable(this.authService.isAuthenticated);

  ngOnInit() {
    this.isAuthenticated$.pipe(
      switchMap(authorized => {
        if (authorized)
          return combineLatest([
            this.reportsService.loadReports(),
            this.issuesService.loadIssuesOnReportChanged$,
            this.instructionService.loadTasks$,
          ]);
        return from(this.router.navigateByUrl(`auth?returnUrl=${encodeURIComponent(window.location.pathname)}`));
      }),
    ).subscribe();
  }
}
