import {ChangeDetectionStrategy, Component} from '@angular/core';
import {Header} from '../../components/header/header';
import {TuiAvatar} from '@taiga-ui/kit';
import {TuiAppearance, TuiButton, TuiScrollbar, TuiTitle} from '@taiga-ui/core';
import {ReactiveFormsModule} from '@angular/forms';
import {TuiCard} from '@taiga-ui/layout';
import {FileSpOptions} from '../../components/source-provider-options/file-sp-options/file-sp-options';
import {GithubSpOptions} from '../../components/source-provider-options/github-sp-options/github-sp-options';
import {RouterLink} from '@angular/router';

interface SourceProvider {
  key: string;
  name: string;
  description: string;
  icon: string;
}

@Component({
  selector: 'app-new-report.page',
  imports: [
    Header,
    TuiTitle,
    ReactiveFormsModule,
    TuiScrollbar,
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
export class NewReportPage {
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

  protected selectProvider(provider: SourceProvider) {
    this.selectedProvider = provider;
  }

  protected deselectProvider() {
    this.selectedProvider = undefined;
  }
}
