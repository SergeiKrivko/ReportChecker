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
| `Ai__ApiUrl` | заполнить (OpenAI-совместимый эндпоинт) |
| `Ai__ApiKey` | **секрет, заполнить** |
| `Ai__DefaultModelId` | заполнить |
| `S3__AccessKey` / `S3__SecretKey` | **секреты, заполнить** |
| `S3__ServiceUrl` / `S3__AuthorizationRegion` | заполнить (S3-совместимое хранилище) |
| `S3__Bucket__Sources` / `S3__Bucket__LocalSources` | `sources` / `local` |
| `YooMoney__WalletId` / `YooMoney__AccessToken` | заполнить (секрет — токен) |
| `YooMoney__PaymentRedirectUrl` | заполнить |

## Ограничение фронтенда

Образ `ghcr.io/sergeikrivko/reportchecker-frontend:master` собран со «вшитыми» адресами:
API — `https://api.reportchecker.ru`, auth — `https://auth.nachert.art`
(`ReportChecker/src/app/app.config.ts`, `src/app/auth/auth.config.ts`). Поэтому UI на новых
доменах будет обращаться к старому бэкенду/авторизации, пока образ не пересобран с новыми
адресами:

- `API_BASE_URL` → `https://reportchecker-api.reportchecker.ru` (или `https://api.reportchecker.ru`
  после переключения DNS);
- issuer/baseUrl auth → `https://auth.reportchecker.ru/api/v1` / `https://auth.reportchecker.ru/`;
- в Avalux Auth в `RedirectUrls` приложения ReportChecker добавить
  `https://reportchecker-ui.reportchecker.ru/auth/callback`.