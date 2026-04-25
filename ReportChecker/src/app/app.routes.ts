import {Routes} from '@angular/router';
import {HomePage} from './pages/home.page/home.page';
import {AuthPage} from './pages/auth.page/auth.page';
import {AuthRedirectPage} from './pages/auth-redirect.page/auth-redirect.page';
import {ReportPage} from './pages/report.page/report.page';
import {SettingsPage} from './pages/settings.page/settings-page.component';
import {ReportRootPage} from './pages/report-root.page/report-root.page';
import {IssuePage} from './pages/issue.page/issue.page';
import {RootPage} from './pages/root.page/root.page';
import {NewReportPage} from './pages/new-report.page/new-report.page';
import {LandingPage} from './pages/landing-page/landing-page';
import {SubscriptionPlansPage} from './pages/subscription-plans.page/subscription-plans.page';
import {NewSubscriptionPage} from './pages/new-subscription.page/new-subscription.page';
import {AdminRootPage} from './pages/admin-root.page/admin-root.page';
import {AdminModelsPage} from './pages/admin-models.page/admin-models.page';
import {AdminSubscriptionsPage} from './pages/admin-subscriptions.page/admin-subscriptions.page';

export const routes: Routes = [
  {path: '', pathMatch: "full", component: LandingPage},
  {
    path: "reports", component: RootPage, children: [
      {path: "", pathMatch: "full", component: HomePage},
      {path: "new", pathMatch: "full", component: NewReportPage},
      {
        path: ":id", component: ReportRootPage, children: [
          {path: "", pathMatch: "full", component: ReportPage},
          {path: "issues/:issueId", pathMatch: "full", component: IssuePage},
          {path: "settings", pathMatch: "full", component: SettingsPage},
        ]
      },
    ]
  },
  {
    path: "admin", component: AdminRootPage, children: [
      {path: "models", pathMatch: "full", component: AdminModelsPage},
      {path: "subscriptions", pathMatch: "full", component: AdminSubscriptionsPage},
    ]
  },
  {path: "auth", pathMatch: "full", component: AuthPage},
  {path: "auth/callback", pathMatch: "full", component: AuthRedirectPage},
  {path: "subscriptions", pathMatch: "full", component: SubscriptionPlansPage},
  {path: "subscriptions/new", pathMatch: "full", component: NewSubscriptionPage},
];
