# ReportChecker

**ReportChecker** — сервис автоматической проверки отчётов и научных работ с помощью ИИ.
Агент читает документ, находит ошибки, спорные места и несоответствия заданным правилам, ведёт
обсуждение по каждому замечанию, предлагает готовые исправления (патчи) и пересматривает их
после загрузки новой версии документа.

Проект — монорепозиторий: backend-API, веб-приложение, десктопный редактор, консольный клиент и
расширение для VS Code.

---

## Содержание

- [Возможности](#возможности)
- [Архитектура](#архитектура)
- [Структура репозитория](#структура-репозитория)
- [Как это работает](#как-это-работает)
- [Форматы и источники](#форматы-и-источники)
- [ИИ-агенты](#ии-агенты)
- [Бенчмарк качества поиска ошибок](#бенчмарк-качества-поиска-ошибок)
- [Технологии](#технологии)
- [Быстрый старт](#быстрый-старт)
- [Конфигурация API](#конфигурация-api)
- [Обзор API](#обзор-api)
- [CI/CD](#cicd)
- [Известные ограничения](#известные-ограничения)

---

## Возможности

- **Проверка через ИИ** — агент читает главы отчёта, находит ошибки, расставляет приоритеты
  (1–10) и формирует комментарии с HTML-форматированием.
- **Повторная проверка версий** — при загрузке новой версии анализируются только изменённые
  главы: старые замечания пересматриваются (`Fixed` / `Closed`), находятся новые.
- **Патчи (исправления)** — для тривиальных ошибок агент предлагает построчный патч
  (`Add` / `Delete` / `Modify`), который можно применить к исходному файлу.
- **Комментарии и диалог** — по каждой ошибке ведётся ветка комментариев; ИИ отвечает на
  сообщения пользователя, уточняет контекст и может предложить исправление.
- **Инструкции на уровень отчёта** — пользователь задаёт, что считать ошибкой, а что игнорировать.
  На основе комментария агент может сам сформулировать инструкцию, сохранить её и применить
  (`Apply`) или искать по ней новые ошибки (`Search`).
- **Три формата** — LaTeX (`.tex`), DOCX, PDF.
- **Три источника** — загрузка файла/архива, локальный клиент (CLI / Studio / VS Code) и GitHub
  (проверка по коммитам и webhooks).
- **Подписки и платежи** — планы с лимитами на токены и число отчётов, бесплатный тариф,
  интеграция с YooMoney.
- **Статистика** — учёт расхода токенов и стоимости по отчётам, моделям и временным интервалам.
- **Бенчмарк моделей** — сравнение моделей по качеству поиска заранее известных ошибок на
  наборе тестов с полным сохранением результатов в БД.
- **Клиенты** — веб-интерфейс, десктопный редактор, консольный клиент и плагин VS Code.

---

## Архитектура

```
                        ┌───────────────────────────────────────────┐
   Веб (Angular)  ─────▶ │                                           │
   CLI (.NET)     ─────▶ │            ReportChecker API              │
   Studio (Avalonia) ──▶ │        ASP.NET Core /api/v1/*             │
   VS Code ext.   ─────▶ │                                           │
                        └───────┬─────────┬─────────┬───────────────┘
                                │         │         │
                       PostgreSQL (EF Core)  Redis   S3-хранилище
                                │
                    ┌───────────┴────────────┐
                    │  AI-агент (OpenAI-API) │
                    └────────────────────────┘
```

API построен по слоистой схеме (Abstractions → Models → DataAccess/Application → Api), а
провайдеры форматов и источников подключаются через интерфейсы `IFormatProvider` и
`ISourceProvider`, что позволяет добавлять новые форматы и источники независимо друг от друга.
Авторизация — OpenID Connect / JWT через **Avalux Auth**.

---

## Структура репозитория

| Каталог | Назначение | Стек |
| --- | --- | --- |
| `Api/` | Backend-сервис | ASP.NET Core, .NET 10 |
| `ReportChecker/` | Веб-приложение | Angular 21, Taiga UI |
| `Studio/` | Десктопный редактор LaTeX-отчётов | Avalonia UI, ReactiveUI |
| `Cli/` | Консольный клиент (загрузка отчёта, авто-применение патчей) | .NET 10, Spectre.Console |
| `VsCode/` | Расширение для VS Code | TypeScript, VS Code API |
| `Shared/` | Общие библиотеки для CLI и Studio | .NET 10 |
| `.github/workflows/` | CI/CD | GitHub Actions |

### Backend (`Api/`)

| Проект | Роль |
| --- | --- |
| `Api` | ASP.NET Core host: контроллеры, схемы запросов, DI, аутентификация |
| `Abstractions` | Интерфейсы сервисов, репозиториев и провайдеров |
| `Models` | Доменные модели (`Report`, `Check`, `Issue`, `Comment`, `Patch`, подписки и др.) |
| `DataAccess` | EF Core: сущности, конфигурации, миграции, репозитории |
| `Application` | Сервисы: проверки, отчёты, патчи, подписки, инструкции, статистика, бенчмарк |
| `AiAgent` | Клиент LLM, промпты агентов, учёт расхода токенов, поиск и сопоставление для бенчмарка |
| `FormatProviders.{Latex,Pdf,Docx}` | Разбор документов на главы и применение патчей |
| `SourceProviders.{File,Local,GitHub}` | Получение исходных файлов отчёта |
| `S3` | Работа с объектным хранилищем |
| `YooMoney` | Платёжный клиент |
| `Exceptions` | Типы доменных исключений |
| `Application.Tests` | Тесты сервисов и репозиториев (NUnit) |

### Frontend (`ReportChecker/`)

Angular-приложение на standalone-компонентах с Taiga UI: маршруты `/reports`, `/reports/new`,
`/reports/:id` (вкладки отчёта, ошибки, настройки), `/admin/models`, `/admin/subscriptions`,
`/statistics`, `/subscriptions`, `/auth`. HTTP-клиент и типы генерируются из Swagger через
**NSwag** (`nswag.json` → `src/app/services/api-client.ts`).

---

## Как это работает

1. **Создание отчёта.** Клиент загружает файл (или архив `.tex`-файлов) и создаёт отчёт с
   указанием формата, источника, LLM-модели и режима обработки изображений.
2. **Разбор на главы.** `IFormatProvider` разбирает документ на главы (для LaTeX — по
   `\chapter`/`\section`/`\subsection`, с переходом по `\include`; для PDF — по закладкам;
   для DOCX — по стилям заголовков). Изображения главы извлекаются отдельно.
3. **Первая проверка.** `AiService.FindIssuesAsync` группирует главы по размеру
   (`Reports.MinChapterSize` / `Reports.MaxRequestSize`), отправляет их агенту и сохраняет
   найденные ошибки, комментарии и патчи.
4. **Обсуждение.** Пользователь комментирует ошибку — агент отвечает (`WriteComment`), может
   поменять статус (`Open` / `Fixed` / `Closed`) и предложить патч.
5. **Инструкции.** Из комментария агент может выделить общее правило, сохранить его как
   инструкцию отчёта и применить к существующим ошибкам или найти по нему новые.
6. **Новая версия.** Загружается новая версия → `DifferenceService` считает разницу глав →
   `CheckIssues` пересматривает старые замечания, `FindIssues` ищет новые. Главы кэшируются в
   Redis на час (`chapters-{checkId}`).
7. **Применение патчей.** CLI/Studio/VS Code применяют патч к локальному файлу, после чего
   статус патча переводится в `Applied` (или `Failed`).

Прогресс проверки отражается статусами: `Queued → InProgress → Completed | Failed | Cancelled`.
Проверку можно отменить (`DELETE .../checks/{checkId}`) или перезапустить, если она упала.

---

## Форматы и источники

### Форматы (`IFormatProvider`)

| Формат | Ключ | Разбор | Патчи |
| --- | --- | --- | --- |
| LaTeX | `Latex` | По секциям, поддержка `\include`, изображения | ✅ |
| PDF | `Pdf` | По закладкам документа (иначе — весь текст как глава `Root`) | — |
| DOCX | `Docx` | По стилям заголовков (outline level) | — |

Разделитель вложенности глав настраивается параметром `Reports.ChapterSeparator` (по умолчанию `//`).

### Источники (`ISourceProvider`)

| Источник | Ключ | Описание |
| --- | --- | --- |
| File | `File` | Файл или ZIP-архив, загруженный в S3-бакет `sources` |
| Local | `Local` | Версии, отправленные CLI / Studio / VS Code (бакет `local`) |
| GitHub | `GitHub` | Файлы репозитория по коммиту; запуск проверок по push-webhook |

Для GitHub API дополнительно создаёт Check Run в репозитории со сводкой найденных ошибок и
ссылкой на веб-версию отчёта.

---

## ИИ-агенты

Все агенты используют OpenAI-совместимый API (`Ai.ApiUrl` / `Ai.ApiKey`) и возвращают
структурированный JSON по заданной схеме. Промпты лежат в `Api/AiAgent/Prompts/*.prompt.txt`.

| Агент | Промпт | Задача |
| --- | --- | --- |
| `FindIssues` | `FindIssues` | Найти новые ошибки в изменённых фрагментах, предложить патч |
| `CheckIssues` | `CheckIssues` | Отметить исправленные / потерявшие актуальность ошибки |
| `WriteComment` | `WriteComment` | Ответить на комментарий пользователя, при необходимости — патч и инструкция |
| `ApplyInstruction` | `ApplyInstruction` | Применить инструкцию к существующим ошибкам |
| `SearchInstruction` | `SearchInstruction` | Найти новые ошибки по инструкции |
| `SearchAny` | `SearchAny` | Свободный поиск ошибок без инструкции |
| `BenchmarkMatchIssues` | `BenchmarkMatchIssues` | Сопоставить оставшиеся найденные ошибки бенчмарка с известными |

Расход учитывается по каждому запросу (`LlmUsage`): модель, тип операции, токены, стоимость,
количество запросов. Лимиты токенов и отчётов зависят от подписки пользователя.

---

## Бенчмарк качества поиска ошибок

Бенчмарк измеряет, насколько хорошо конкретная модель находит заранее известные ошибки.
Прогон — это одна пара «тест + модель»: модель получает текст теста, её находки
сопоставляются с эталонным списком, результат сохраняется в БД целиком (строка на каждую
известную ошибку и на каждую «лишнюю» найденную).

### Тестовые данные

Тесты лежат в каталоге `Api/Api/Benchmarks/<id>` и копируются в выходные файлы приложения:
`case.json` с описанием и списком известных ошибок плюс исходный файл (`entryFile`).
Путь переопределяется ключом `Benchmarks.DataPath`. Полный формат `case.json`, режимы
`displayMode` и правила сопоставления описаны в
[`Api/Api/Benchmarks/README.md`](Api/Api/Benchmarks/README.md).

Готовые тесты: `orthography-basics` (орфография и пунктуация), `structure-references`
(ссылки и терминология), `math-formulas` (формулы, на фронте скрыт).

### Как работает прогон

1. `POST /api/v1/benchmarks/runs` создаёт прогон для каждой пары (тест, модель) в статусе
   `Queued` и запускает их в фоне (не более `Benchmarks.MaxParallelRuns` одновременно).
2. Модель ищет ошибки тем же агентом, что и первая проверка отчёта
   (`FindIssues`, по одному запросу на группу глав).
3. Найденное сопоставляется с известным сначала детерминированно (глава, строка, похожесть
   заголовка), затем остаток — одним запросом `BenchmarkMatchIssues` (если включено
   `Benchmarks.LlmMatching`).
4. Для ошибок с `checkFix` патч модели применяется к тексту главы и сравнивается с эталонным.
5. Результат сохраняется: для каждой известной ошибки — найдена ли, совпал ли заголовок и
   приоритет, статус сравнения исправления, заголовок/комментарий/приоритет найденного;
   для «лишних» — отдельная строка без номера известной ошибки. Сохраняются токены, стоимость
   и время прогона.

Прогон обходит лимиты подписки и не пишется в статистику расхода отчётов (`LlmUsages`) —
расход хранится на самом прогоне. Прогоны удаляются мягко: `DELETE` отменяет активный прогон
и помечает запись удалённой, после чего она пропадает из `GET /runs` и `/summary`.

Ключи конфигурации: `Benchmarks.DataPath` (`Benchmarks`), `Benchmarks.LineTolerance` (`3`),
`Benchmarks.TitleSimilarityThreshold` (`0.6`), `Benchmarks.LlmMatching` (`true`),
`Benchmarks.LlmConfidenceThreshold` (`60`), `Benchmarks.MaxParallelRuns` (`2`).

---

## Технологии

- **Backend:** .NET 10, ASP.NET Core, EF Core + PostgreSQL, StackExchange.Redis, Swashbuckle,
  Octokit (GitHub App), YooMoney, AWS SDK (S3), OpenAI SDK.
- **Frontend:** Angular 21, Taiga UI 4, NgRx Signals, `angular-oauth2-oidc`, NSwag, Vitest.
- **Desktop:** Avalonia 11, AvaloniaEdit + TextMate, ReactiveUI, Lucide.Avalonia, `pdflatex`
  для сборки PDF.
- **CLI:** .NET 10, CommandLineParser, Spectre.Console, NSwag-клиент.
- **VS Code:** TypeScript, esbuild, `diff-match-patch`, `fflate`.
- **Инфраструктура:** Docker, GitHub Actions, GHCR.

---

## Быстрый старт

### Backend (API)

Требуется **.NET 10 SDK**, доступные PostgreSQL, Redis и S3-совместимое хранилище.

```bash
cd Api
dotnet restore
dotnet run --project Api          # профиль http → http://localhost:5131
```

Swagger UI доступен по `/swagger`. NSwag-конфиги (`Cli/Services/nswag.json`,
`ReportChecker/nswag.json`) по умолчанию ожидают API на `http://localhost:5000`.

Обязательные параметры конфигурации см. в разделе [Конфигурация API](#конфигурация-api) —
без них приложение намеренно падает на старте. Миграции применяются автоматически при запуске.

Тесты:

```bash
dotnet test Api/Application.Tests
```

### Веб-приложение

Требуется **Node.js** и npm.

```bash
cd ReportChecker
npm ci
npm start                         # http://localhost:4200
npm run build                     # production-сборка в dist/
npm test                          # Vitest
```

Базовый адрес API задаётся в `src/app/app.config.ts` (`API_BASE_URL`).

### CLI

Требуется **.NET 10 SDK**.

```bash
dotnet run --project Cli/Cli -- <путь-к-файлу-отчёта> [--new] [--logout]
```

Клиент авторизуется через Avalux Auth, загружает файл (или ZIP) как источник `Local`, после
чего следит за изменениями файлов, автоматически отправляет новые версии и применяет патчи,
пришедшие от ИИ. Настройки и привязка путей к отчётам хранятся в
`%LocalAppData%/SergeiKrivko/ReportChecker/settings.xml`.

### Studio

Требуется **.NET 10 SDK**; для сборки PDF — установленный `pdflatex`.

```bash
dotnet run --project Studio/Studio
```

Десктопный редактор LaTeX с деревом проекта, подсветкой, автодополнением команд и окружений,
панелью ошибок, комментариями и предпросмотром.

### Расширение VS Code

Требуется **Node.js ≥ 20**. Подробности — в [`VsCode/README.md`](VsCode/README.md).

```bash
cd VsCode
npm install
npm run compile        # сборка бандла (esbuild)
npm run typecheck
npm test               # тесты latex-провайдера
```

Отладка: открыть папку `VsCode` в VS Code и нажать **F5**.

---

## Конфигурация API

Параметры читаются из `appsettings*.json`, переменных окружения (`AddEnvironmentVariables`,
разделитель `__`) или секретов. Ключи:

| Ключ | Назначение |
| --- | --- |
| `ConnectionStrings.Postgres` | Строка подключения к PostgreSQL |
| `ConnectionStrings.Redis` | Подключение к Redis (по умолчанию `localhost:6381`) |
| `Security.AuthApiUrl` | Базовый адрес Avalux Auth (OIDC-discovery) |
| `Security.ApiToken` | Сервисный токен для Auth API |
| `Security.Issuer` / `Security.Audience` | Проверка JWT |
| `Security.AdminIds` | Список ID администраторов через `;` |
| `Security.CorsOrigins` | Дополнительные разрешённые origin через `;` |
| `Frontend.Url` | Адрес веб-версии (CORS, ссылки в GitHub Check Run) |
| `GitHub.AppName` / `GitHub.AppId` | Параметры GitHub App |
| `GitHub.PrivateKey` | Переменная окружения с приватным ключом GitHub App |
| `GitHub.WebhookSecret` | Секрет webhook (обязателен) |
| `Ai.ApiUrl` / `Ai.ApiKey` | OpenAI-совместимый эндпоинт и ключ |
| `Ai.DefaultModelId` | Модель по умолчанию |
| `S3.AccessKey` / `S3.SecretKey` / `S3.ServiceUrl` / `S3.AuthorizationRegion` | Доступ к S3 |
| `S3.Bucket.Sources` / `S3.Bucket.LocalSources` | Бакеты (по умолчанию `sources` / `local`) |
| `YooMoney.WalletId` / `YooMoney.AccessToken` / `YooMoney.PaymentRedirectUrl` | Приём платежей |
| `Subscriptions.Free.Name` / `.Description` / `.Tokens` / `.Reports` | Параметры бесплатного тарифа |
| `Reports.ChapterSeparator` | Разделитель вложенности глав (по умолчанию `//`) |
| `Reports.MinChapterSize` / `Reports.MaxRequestSize` | Границы группировки глав для LLM |
| `Benchmarks.DataPath` | Каталог тестов бенчмарка (по умолчанию `Benchmarks` рядом с приложением) |
| `Benchmarks.LineTolerance` | Допустимая разница строк при детерминированном сопоставлении (по умолчанию `3`) |
| `Benchmarks.TitleSimilarityThreshold` | Порог похожести заголовков (по умолчанию `0.6`) |
| `Benchmarks.LlmMatching` / `Benchmarks.LlmConfidenceThreshold` | LLM-досопоставление остатка и порог уверенности (по умолчанию `true` / `60`) |
| `Benchmarks.MaxParallelRuns` | Сколько прогонов бенчмарка выполнять одновременно (по умолчанию `2`) |

---

## Обзор API

Базовый путь — `/api/v1`, авторизация — `Authorization: Bearer <JWT>`.

| Группа | Маршрут |
| --- | --- |
| Отчёты | `/reports`, `/reports/{id}`, `/reports/test-source` |
| Проверки | `/reports/{reportId}/checks`, `.../checks/latest`, `.../latest/chapters`, `.../latest/restart` |
| Ошибки | `/reports/{reportId}/issues` |
| Комментарии | `/reports/{reportId}/issues/{issueId}/comments` (+ `read`, статусы патчей) |
| Инструкции | `/reports/{reportId}/instructions`, `.../instructions/tasks` |
| Файлы | `/files` (загрузка, ссылка на скачивание) |
| GitHub | `/github/repositories`, `/github/branches`, `/github/files`, `/github/installation` |
| Модели | `/models` (чтение — анонимно, изменение — только админ) |
| Подписки | `/subscriptions`, `/subscriptions/current`, `/subscriptions/plans`, `.../offers` |
| Статистика | `/statistics/usage`, `/statistics/timeUsage` |
| Бенчмарк | `/benchmarks/cases`, `/benchmarks/runs` (+ `{id}`), `/benchmarks/summary` |
| Webhook | `POST /api/v1/github/webhooks` (подпись проверяется секретом) |

Политики авторизации: `User` (требуется claim `UserId`) и `Admin` (claim входит в `Security.AdminIds`).
Ошибки возвращаются в формате Problem Details.

---

## CI/CD

| Workflow | Триггер | Действие |
| --- | --- | --- |
| `main.yml` | изменения в `Api/**` | Сборка Docker-образа API и push в GHCR |
| `frontend.yml` | изменения в `ReportChecker/**` | Сборка Docker-образа веб-приложения и push в GHCR |
| `cli.yml` | изменение `Cli/Cli/Cli.csproj` | Публикация сборок CLI под win/linux/osx-x64 |
| `vscode.yml` | изменения в `VsCode/**` | Typecheck, тесты, `.vsix`, публикация в VS Code Marketplace по новой версии |

---

## Известные ограничения

- **CLI не собирается из текущего дерева.** `Cli/Cli/Cli.csproj` ссылается на проект
  `Cli/FormatProviders/Cli.FormatProviders.Latex`, которого нет в репозитории (остались только
  артефакты `obj/`). Для сборки CLI этот проект нужно восстановить.
- **Патчи поддерживает только LaTeX.** PDF и DOCX разбираются и проверяются, но применить
  исправление к ним нельзя.
- **Патчи применяются без отката** — перед применением исправления рекомендуется закоммитить
  изменения.
- **Расширение VS Code** работает только с LaTeX и источником `Local`.
- В корне лежат временные каталоги (`.tmp-*`) и скретч-файл `test.cs`, не относящиеся к сборке.

---

## Лицензия

Расширение VS Code распространяется под лицензией MIT (см. `VsCode/package.json`).
