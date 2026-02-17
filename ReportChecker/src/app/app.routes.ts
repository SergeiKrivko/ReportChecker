import {Routes} from '@angular/router';
import {HomePage} from './pages/home.page/home.page';
import {RootPage} from './pages/root.page/root.page';
import {AuthPage} from './pages/auth.page/auth.page';
import {AuthRedirectPage} from './pages/auth-redirect.page/auth-redirect.page';
import {ReportPage} from './pages/report.page/report.page';
import {InstructionsPage} from './pages/instructions.page/instructions.page';
import {ReportRootPage} from './pages/report-root.page/report-root.page';
import {VersionsPage} from './pages/versions.page/versions.page';

export const routes: Routes = [
  {
    path: "", component: RootPage, children: [
      {path: "", pathMatch: "full", component: HomePage},
      {
        path: "reports/:id", component: ReportRootPage, children: [
          {path: "issues", pathMatch: "full", component: ReportPage},
          {path: "versions", pathMatch: "full", component: VersionsPage},
          {path: "instructions", pathMatch: "full", component: InstructionsPage},
        ]
      },
    ]
  },
  {path: "auth", pathMatch: "full", component: AuthPage},
  {path: "auth/callback", pathMatch: "full", component: AuthRedirectPage},
];
