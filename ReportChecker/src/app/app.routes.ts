import {Routes} from '@angular/router';
import {HomePage} from './pages/home.page/home.page';
import {AuthPage} from './pages/auth.page/auth.page';
import {AuthRedirectPage} from './pages/auth-redirect.page/auth-redirect.page';
import {ReportPage} from './pages/report.page/report.page';
import {SettingsPage} from './pages/settings.page/settings-page.component';
import {ReportRootPage} from './pages/report-root.page/report-root.page';
import {IssuePage} from './pages/issue.page/issue.page';
import {RootPage} from './pages/root.page/root.page';

export const routes: Routes = [
  {
    path: "", component: RootPage, children: [
      {path: "", pathMatch: "full", component: HomePage},
      {
        path: "reports/:id", component: ReportRootPage, children: [
          {path: "", pathMatch: "full", component: ReportPage},
          {path: "issues/:issueId", pathMatch: "full", component: IssuePage},
          {path: "settings", pathMatch: "full", component: SettingsPage},
        ]
      },
    ]
  },
  {path: "auth", pathMatch: "full", component: AuthPage},
  {path: "auth/callback", pathMatch: "full", component: AuthRedirectPage},
];
