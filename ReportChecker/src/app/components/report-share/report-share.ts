import {ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnInit, signal} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {FormsModule, FormControl, ReactiveFormsModule} from '@angular/forms';
import {filter, switchMap} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {TuiAlertService, TuiButton, TuiLoader, TuiTextfield} from '@taiga-ui/core';
import {
  TUI_CONFIRM,
  TuiAvatar,
  TuiBadge,
  TuiConfirmData,
  TuiDataListWrapperComponent,
  TuiSelectDirective
} from '@taiga-ui/kit';
import {TuiResponsiveDialogService} from '@taiga-ui/addon-mobile';
import {API_BASE_URL, ReportAccessLevel} from '../../services/api-client';
import {
  ReportShareEntity,
  ReportShareLinkEntity,
  ReportShareService
} from '../../services/report-share.service';

@Component({
  selector: 'app-report-share',
  imports: [
    AsyncPipe,
    FormsModule,
    ReactiveFormsModule,
    TuiButton,
    TuiLoader,
    TuiTextfield,
    TuiAvatar,
    TuiBadge,
    TuiSelectDirective,
    TuiDataListWrapperComponent,
  ],
  templateUrl: './report-share.html',
  styleUrl: './report-share.scss',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportShare implements OnInit {
  private readonly reportShareService = inject(ReportShareService);
  private readonly dialogs = inject(TuiResponsiveDialogService);
  private readonly alerts = inject(TuiAlertService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  readonly reportId = input.required<string>();

  protected readonly shares$ = this.reportShareService.shares$;
  protected readonly link$ = this.reportShareService.link$;
  protected readonly loaded$ = this.reportShareService.loaded$;

  protected readonly inviteControl = new FormControl<string>('', {nonNullable: true});
  protected readonly accessControl = new FormControl<ReportAccessLevel>(
    ReportAccessLevel.Read,
    {nonNullable: true},
  );
  protected readonly busy = signal(false);

  protected readonly accessLevels: ReportAccessLevel[] = [
    ReportAccessLevel.Read,
    ReportAccessLevel.Comment,
    ReportAccessLevel.Edit,
  ];

  ngOnInit() {
    this.loadShares();
  }

  protected stringifyAccessLevel = (level: ReportAccessLevel): string => accessLevelLabels[level] ?? '???';

  protected shareName(share: ReportShareEntity): string {
    return share.name ?? share.login ?? share.email ?? 'Пользователь';
  }

  protected shareSecondary(share: ReportShareEntity): string {
    if (share.email && share.email !== share.name)
      return share.email;
    if (share.login && share.login !== share.name)
      return share.login;
    return '';
  }

  protected shareUrl(link: ReportShareLinkEntity): string {
    return `${this.apiBaseUrl.replace(/\/+$/, '')}/api/v1/shared/reports/${encodeURIComponent(link.token)}`;
  }

  protected invite() {
    const loginOrEmail = this.inviteControl.value.trim();
    if (!loginOrEmail || this.busy())
      return;

    this.busy.set(true);
    this.reportShareService.share(this.reportId(), loginOrEmail, this.accessControl.value).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.busy.set(false);
        this.inviteControl.setValue('');
        this.notify('Доступ выдан', 'Пользователь добавлен');
      },
      error: () => this.busy.set(false),
    });
  }

  protected changeAccess(share: ReportShareEntity, accessLevel: ReportAccessLevel) {
    if (accessLevel === share.accessLevel)
      return;

    this.reportShareService.update(this.reportId(), share.userId, accessLevel).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.notify('Доступ обновлён', 'Уровень доступа изменён'),
      error: () => this.loadShares(),
    });
  }

  protected revoke(share: ReportShareEntity) {
    const data: TuiConfirmData = {
      content: `Отозвать доступ пользователя «${this.shareName(share)}»?`,
      yes: 'Отозвать',
      no: 'Отмена',
    };

    this.dialogs.open<boolean>(TUI_CONFIRM, {size: 's', data}).pipe(
      filter(Boolean),
      switchMap(() => this.reportShareService.revoke(this.reportId(), share.userId)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.notify('Доступ отозван', 'Пользователь больше не видит отчёт'),
    });
  }

  protected createLink() {
    this.reportShareService.createLink(this.reportId()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.notify('Ссылка готова', 'Публичная ссылка создана'),
    });
  }

  protected rotateLink() {
    const data: TuiConfirmData = {
      content: 'Перевыпустить публичную ссылку? Старая ссылка перестанет работать.',
      yes: 'Перевыпустить',
      no: 'Отмена',
    };

    this.dialogs.open<boolean>(TUI_CONFIRM, {size: 's', data}).pipe(
      filter(Boolean),
      switchMap(() => this.reportShareService.rotateLink(this.reportId())),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.notify('Ссылка перевыпущена', 'Старая ссылка больше не работает'),
    });
  }

  protected revokeLink() {
    const data: TuiConfirmData = {
      content: 'Отозвать публичную ссылку? Открыть отчёт по ней больше не получится.',
      yes: 'Отозвать',
      no: 'Отмена',
    };

    this.dialogs.open<boolean>(TUI_CONFIRM, {size: 's', data}).pipe(
      filter(Boolean),
      switchMap(() => this.reportShareService.revokeLink(this.reportId())),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => this.notify('Ссылка отозвана', 'Публичная ссылка удалена'),
    });
  }

  protected copyLink(link: ReportShareLinkEntity) {
    const url = this.shareUrl(link);
    if (!navigator.clipboard) {
      this.notify('Браузер не поддерживает копирование', 'Ошибка', 'negative');
      return;
    }
    navigator.clipboard.writeText(url).then(
      () => this.notify('Ссылка скопирована', 'Скопировано в буфер обмена'),
      () => this.notify('Не удалось скопировать ссылку', 'Ошибка', 'negative'),
    );
  }

  private loadShares() {
    this.reportShareService.load(this.reportId()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe();
  }

  private notify(message: string, label: string, appearance: 'positive' | 'negative' = 'positive') {
    this.alerts.open(message, {label, appearance}).subscribe();
  }
}

const accessLevelLabels: Record<ReportAccessLevel, string> = {
  [ReportAccessLevel.Read]: 'Чтение',
  [ReportAccessLevel.Comment]: 'Комментарии',
  [ReportAccessLevel.Edit]: 'Редактирование',
  [ReportAccessLevel.Owner]: 'Владелец',
};
