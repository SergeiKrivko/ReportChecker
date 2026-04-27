import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {RouterLink} from '@angular/router';
import {map} from 'rxjs';
import {IssuesService} from '../../services/issues.service';
import {AsyncPipe} from '@angular/common';
import {TuiButton, TuiIcon, TuiLoader, TuiSurface} from '@taiga-ui/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {FileEntity} from '../../entities/file-entity';
import {IssueHeader} from '../../components/issue-header/issue-header';
import {TuiCard} from '@taiga-ui/layout';
import {ReportsService} from '../../services/reports.service';
import {FileSpVersion} from '../../components/file-sp-version/file-sp-version';
import {GithubSpVersion} from '../../components/github-sp-version/github-sp-version';
import {InstructionService} from '../../services/instruction.service';
import {TuiSegmented} from '@taiga-ui/kit';

@Component({
  selector: 'app-check.page',
  imports: [
    AsyncPipe,
    TuiButton,
    ReactiveFormsModule,
    TuiLoader,
    IssueHeader,
    RouterLink,
    TuiCard,
    TuiSurface,
    FileSpVersion,
    GithubSpVersion,
    TuiSegmented,
    TuiIcon,
  ],
  templateUrl: './report.page.html',
  styleUrl: './report.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportPage implements OnInit {
  private readonly issuesService = inject(IssuesService);
  private readonly reportsService = inject(ReportsService);
  private readonly instructionService = inject(InstructionService);

  protected readonly selectedReport$ = this.reportsService.selectedReport$;
  protected readonly issues$ = this.issuesService.issues$.pipe(
    map(issues => issues.sort((a, b) => a.priority - b.priority))
  );
  protected readonly activeIssues$ = this.issuesService.allIssues$.pipe(
    map(issues => issues.filter(e => e.status == "Open"))
  );
  protected readonly closedIssues$ = this.issuesService.allIssues$.pipe(
    map(issues => issues.filter(e => e.status == "Closed"))
  );
  protected readonly fixedIssues$ = this.issuesService.allIssues$.pipe(
    map(issues => issues.filter(e => e.status == "Fixed"))
  );
  protected readonly isProgress$ = this.issuesService.isProgress$;
  protected readonly instructionTasks$ = this.instructionService.tasks$;

  protected readonly control = new FormControl<FileEntity | null>(null);

  ngOnInit() {
    this.issuesService.deselectIssue();
  }

  protected selectStatus(status: string | null) {
    this.issuesService.selectStatus(status);
  }
}
