import {Routes} from '@angular/router';
import {HomePage} from './pages/home.page/home.page';
import {RootPage} from './pages/root.page/root.page';
import {AuthPage} from './pages/auth.page/auth.page';
import {AuthRedirectPage} from './pages/auth-redirect.page/auth-redirect.page';
import {ReportPage} from './pages/report.page/report.page';

export const routes: Routes = [
  {
    path: "", component: RootPage, children: [
      {path: "", pathMatch: "full", component: HomePage},
      {path: "reports/:id", pathMatch: "full", component: ReportPage},
    ]
  },
  {path: "auth", pathMatch: "full", component: AuthPage},
  {path: "auth/callback", pathMatch: "full", component: AuthRedirectPage},
];
