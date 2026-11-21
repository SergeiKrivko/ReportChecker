# Развёртывание ReportChecker на этой ВМ

Стек создан в Portainer как `reportchecker` (id 11). Compose-файл —
[`docker-compose.yml`](./docker-compose.yml); переменные окружения задаются в Portainer:
**Stacks → reportchecker → Environment variables** (таблица ниже). После заполнения секретов
нажмите **Update the stack** — контейнеры `reportchecker-postgres` и `reportchecker-api`
пересоздадутся с новыми значениями.

Сейчас стек в статусе `failed` (ожидаемо): пустой `DB__PASS` не даёт PostgreSQL
инициализироваться, а пустой `GitHub__WebhookSecret` не даёт API стартовать. UI и Redis уже
работают.

## Домены

| Что | Домен | Статус |
| --- | --- | --- |
| UI | `reportchecker.ru`, `www.reportchecker.ru` | DNS пока указывает на старую машину; роутеры уже прописаны, сертификат выпустится после переключения DNS |
| UI | `reportchecker-ui.reportchecker.ru` | работает сразу (`*.reportchecker.ru` указывает на эту ВМ) |
| API | `api.reportchecker.ru` | DNS пока указывает на старую машину; роутер уже прописан |
| API | `reportchecker-api.reportchecker.ru` | роутер и домен готовы, API поднимется после заполнения секретов |

Traefik (стек `infra`) сам выпускает сертификаты Let's Encrypt (HTTP-01) для каждого хоста.
Домены `*.nachert.art` из старой конфигурации намеренно не добавлены: сейчас они указывают
на старую машину (176.109.106.249). Сети `monitoring` на этой ВМ нет, поэтому promtail-лейблы
из исходного compose убраны.

## Переменные окружения

Ключи .NET-конфигурации используют разделитель `__` (например, `ConnectionStrings__Postgres`).

| Переменная | Значение по умолчанию / комментарий |
| --- | --- |
| `DB__NAME` | `reportchecker` |
| `DB__USER` | `reportchecker` |
| `DB__PASS` | **секрет, заполнить** |
| `Security__AuthApiUrl` | `https://auth.reportchecker.ru` |
| `Security__Issuer` | `https://auth.reportchecker.ru/api/v1` |
| `Security__Audience` | `7c4a1272396979451d2a2d311f087050` (ClientId приложения ReportChecker в Avalux Auth) |
| `Security__AdminIds` | `b13fa26b-0a30-4558-a2cd-da2d68022bab` (`;`-разделённый список) |
| `Security__CorsOrigins` | `https://www.reportchecker.ru;https://reportchecker-ui.reportchecker.ru` |
| `Security__ApiToken` | **секрет, заполнить** (сервисный токен Auth API) |
| `Frontend__Url` | `https://reportchecker.ru` |
| `GitHub__AppId` | заполнить (числовой App ID) |
| `GitHub__PrivateKey` | **секрет, заполнить** (PEM-ключ GitHub App) |
| `GitHub__WebhookSecret` | **секрет, заполнить** (без него API не стартует) |
| `YandexDisk__PollingIntervalMinutes` | `10` — как часто сверять отчёты с Диском; `0` выключает автопроверку |
| `YandexDisk__MaxSnapshotBytes` | `209715200` (200 МБ) — предел размера снимка |
| `GoogleDrive__PollingIntervalMinutes` | `10` — как часто сверять отчёты с Google Диском; `0` выключает автопроверку |
| `GoogleDrive__MaxSnapshotBytes` | `209715200` (200 МБ) — предел размера снимка |
| `Ai__ApiUrl` | заполнить (OpenAI-совместимый эндпоинт) |
| `Ai__ApiKey` | **секрет, заполнить** |
| `Ai__DefaultModelId` | заполнить |
| `S3__AccessKey` / `S3__SecretKey` | **секреты, заполнить** |
| `S3__ServiceUrl` / `S3__AuthorizationRegion` | заполнить (S3-совместимое хранилище) |
| `S3__Bucket__Sources` / `S3__Bucket__LocalSources` | `sources` / `local` |
| `YooMoney__WalletId` / `YooMoney__AccessToken` | заполнить (секрет — токен) |
| `YooMoney__PaymentRedirectUrl` | заполнить |

## Яндекс.Диск

Отдельного OAuth-приложения для Диска не нужно: токен хранится в **Avalux Auth** вместе с
привязанным Яндекс-аккаунтом и обновляется сервисом авторизации автоматически. Пользователь
привязывает Яндекс на странице «Аккаунт» (вход через провайдера `yandex`), после чего может
создавать отчёты из Диска. Без привязанного аккаунта или выданного токена API отклоняет создание
отчёта.

`YandexDisk__PollingIntervalMinutes` (по умолчанию `10`) задаёт частоту фоновой сверки с Диском;
`0` выключает её — ручная кнопка «Проверить» продолжает работать. `YandexDisk__MaxSnapshotBytes`
ограничивает размер снимка версии.

## Google Диск

Отдельного OAuth-приложения для Диска не нужно: токен хранится в **Avalux Auth** вместе с
привязанным Google-аккаунтом и обновляется сервисом авторизации автоматически. Пользователь
привязывает Google на странице «Аккаунт» (вход через провайдера `google`), после чего может
создавать отчёты с Google Диска. Без привязанного аккаунта или выданного токена API отклоняет
создание отчёта.

В настройках провайдера `google` в Avalux Auth в список scope нужно добавить
`https://www.googleapis.com/auth/drive.readonly` (чтение файлов и метаданных Диска) и включить
сохранение токенов (`SaveTokens`), иначе Google не выдаст refresh token и поллинг перестанет
работать после истечения access token.

`GoogleDrive__PollingIntervalMinutes` (по умолчанию `10`) задаёт частоту фоновой сверки с Диском;
`0` выключает её — ручная кнопка «Проверить» продолжает работать. `GoogleDrive__MaxSnapshotBytes`
ограничивает размер снимка версии.

### Пока приложение Google не проверено: тестовые пользователи

`drive.readonly` — restricted scope, поэтому непроверенное приложение не может открыть Диск
произвольному аккаунту. Пока Google-приложение не прошло проверку, доступ получают только
аккаунты из списка тестировщиков (до 100). Список ведётся в том Google Cloud проекте, где создан
OAuth-клиент для провайдера `google` в Avalux Auth:

1. Консоль Google Cloud → нужный проект → **Google Auth Platform → Audience**
   (`https://console.cloud.google.com/auth/audience`).
2. User type = **External**, Publishing status = **Testing**.
3. **Test users → + Add users** → точный адрес Google-аккаунта (личный Gmail или адрес
   Workspace) → **Save**. Группы и алиасы не подходят — нужен адрес, под которым человек входит.
4. На странице **Data Access** (`/auth/scopes`) в scope должны быть `.../auth/drive.readonly`,
   а в **APIs & Services → Library** включён **Google Drive API** (иначе Drive API отвечает 403).
5. Пользователь заново входит через Google на странице «Аккаунт»: при первом входе Google
   показывает «Google hasn't verified this app» → **Advanced → Go to … (unsafe)**, после чего
   выдаёт согласие на чтение Диска. Avalux Auth при повторном входе отзывает старый refresh token
   и сохраняет новый — уже с нужным scope.

Ограничения режима **Testing** (external user type):

- аккаунт вне списка тестировщиков получает «Access blocked: … has not completed the Google
  verification process» — это не ошибка приложения;
- **refresh token живёт 7 дней**: после этого фоновая сверка отваливается (в логах — предупреждение
  «Google Drive is not connected…», в UI — «привяжите Google-аккаунт»), пока пользователь снова не
  войдёт через Google. Для постоянной работы выберите один из вариантов:
  - Publishing status = **In production**: для restricted scope нужна проверка OAuth-приложения и
    ежегодная независимая оценка безопасности (CASA), плюс домен, политика конфиденциальности и
    демо-видео;
  - User type = **Internal**, если аккаунты — Google Workspace одного домена: проверка не нужна,
    тестовые пользователи и 7-дневное ограничение не действуют (для личных Gmail не подходит);
  - отказаться от restricted scope в пользу `drive.file` — проверка не нужна, но приложение видит
    только файлы, которые само создало или которые пользователь выбрал через Google Picker;
    текущий браузер папок (`files.list` по всему Диску) с этим scope работать не будет.

## Ограничение фронтенда

Образ `ghcr.io/sergeikrivko/reportchecker-frontend:master` собран со «вшитыми» адресами
(`ReportChecker/src/app/app.config.ts`, `src/app/auth/auth.config.ts`), поэтому после правки
исходников образ нужно пересобрать и перезапустить. Текущее состояние:

- auth в исходниках переведён на `https://auth.reportchecker.ru/api/v1` /
  `https://auth.reportchecker.ru/`;
- `API_BASE_URL` пока `https://api.reportchecker.ru` — заменить на
  `https://reportchecker-api.reportchecker.ru` (или оставить после переключения DNS);
- в Avalux Auth в `RedirectUrls` приложения ReportChecker добавить
  `https://reportchecker-ui.reportchecker.ru/auth/callback`.