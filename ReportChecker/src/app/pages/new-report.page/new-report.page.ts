import {ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit} from '@angular/core';
import {TuiAvatar} from '@taiga-ui/kit';
import {TuiAppearance, TuiButton, TuiTitle} from '@taiga-ui/core';
import {ReactiveFormsModule} from '@angular/forms';
import {TuiCard} from '@taiga-ui/layout';
import {FileSpOptions} from '../../components/source-provider-options/file-sp-options/file-sp-options';
import {GithubSpOptions} from '../../components/source-provider-options/github-sp-options/github-sp-options';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {map, tap} from 'rxjs';

interface SourceProvider {
  key: string;
  name: string;
  description: string;
  icon: string;
}

@Component({
  selector: 'app-new-report.page',
  imports: [
    TuiTitle,
    ReactiveFormsModule,
    TuiAvatar,
    TuiCard,
    TuiAppearance,
    FileSpOptions,
    GithubSpOptions,
    TuiButton,
    RouterLink,
  ],
  templateUrl: './new-report.page.html',
  styleUrl: './new-report.page.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewReportPage implements OnInit {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected direction = 0;

  protected readonly sourceProviders$: SourceProvider[] = [
    {
      key: "File",
      name: "Файл",
      description: "Загрузка файла через интерфейс. Можно будет загрузить новую версию файла",
      icon: "@tui.file"
    },
    {
      key: "GitHub",
      name: "GitHub",
      description: "Использование файла/файлов из репозитория GitHub. После push новая версия будет проверена автоматически",
      icon: "github.svg"
    },
  ];

  protected selectedProvider: SourceProvider | undefined;

  ngOnInit() {
    this.activatedRoute.queryParamMap.pipe(
      map(params => params.get("source")),
      tap(source => {
        this.selectedProvider = this.sourceProviders$.find(e => e.key == source);
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  protected selectProvider(provider?: SourceProvider) {
    this.selectedProvider = provider;
    void this.router.navigateByUrl(`/reports/new?source=${provider?.key}`);
  }

  protected deselectProvider() {
    this.selectedProvider = undefined;
    void this.router.navigateByUrl('/reports/new');
  }
}
