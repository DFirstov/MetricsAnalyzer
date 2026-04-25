# Metrics Analyzer

Metrics Analyzer — заготовка микросервисного проекта для анализа метрик и предсказания аварийных ситуаций.

Сейчас в репозитории есть локальная инфраструктура, стартовые .NET-сервисы и рабочее `MarketData.Api` с PostgreSQL через EF Core и кэшированием через Redis.

## Что уже настроено

### Инфраструктура

- **PostgreSQL** — основное хранилище данных (localhost:5432).
- **Valkey (бесплатный Redis)** — in-memory кэш (localhost:6379).
- **Kafka** — брокер сообщений (localhost:9092).
- **Инициализация Kafka-топика** — при старте поднимается `trade-events` (если его ещё нет).

### Сервисы

- `Services/Trading.Api` — HTTP API-заготовка (http://localhost:5194).
- `Services/MarketData.Api` — HTTP API с PostgreSQL (EF Core), авто-применением миграций при старте и кэшированием в Redis (http://localhost:5117/swagger).
- `Services/Notification.Worker` — worker-заготовка (http://localhost:5086).

На текущем этапе `Trading.Api` и `Notification.Worker` остаются сервисами-заготовками, а в `MarketData.Api` уже реализованы рабочие endpoints для цен акций.

## Что умеет MarketData.Api сейчас

- `POST /api/stocks/seed` — одноразово заполняет БД тестовыми акциями (`AAPL`, `MSFT`, `GOOGL`).
- `GET /api/stocks/{ticker}` — возвращает данные акции по тикеру (поиск без учёта регистра).
- Для `GET /api/stocks/{ticker}` используется стратегия cache-aside:
  1) сначала чтение из Redis по ключу `price:{TICKER}`,
  2) при cache miss — чтение из PostgreSQL,
  3) результат кладётся в Redis на 1 минуту.
- Если тикер не найден, API возвращает `404` с сообщением `Ticker not found`.
- Если сидирование уже выполнялось, `POST /api/stocks/seed` возвращает `400` (`Already seeded`).

## Быстрый старт

### Требования

- Docker
- Docker Compose
- .NET SDK 10.0+

### 1) Запуск только инфраструктуры

```bash
docker compose --profile infra up -d
```

### 2) Запуск инфраструктуры и сервисов в Docker

```bash
docker compose --profile app up -d --build
```

## Структура проекта

- `docker-compose.yml` — локальная инфраструктура и запуск сервисов в Docker через профили `infra` и `app`.
- `MetricsAnalyzer.slnx` — solution-файл.
- `Services/Trading.Api` — API для домена торговых операций.
- `Services/MarketData.Api` — API для рыночных данных (EF Core + PostgreSQL + Redis + миграции + контроллер акций).
- `Services/Notification.Worker` — сервис фоновой обработки и уведомлений.

## Текущее состояние

Проект находится на стадии каркаса: инфраструктура и сервисы подготовлены, есть контейнеризация сервисов, а в `MarketData.Api` уже реализован базовый сценарий работы с акциями (сидирование, чтение по тикеру, кэширование в Redis, авто-миграции БД). Бизнес-логика будет расширяться в следующих итерациях.
