# Metrics Analyzer

Metrics Analyzer — заготовка микросервисного проекта для анализа метрик и предсказания аварийных ситуаций.

Сейчас в репозитории есть локальная инфраструктура, стартовые .NET-сервисы и базовая интеграция `MarketData.Api` с PostgreSQL через EF Core.

## Что уже настроено

### Инфраструктура

- **PostgreSQL** — основное хранилище данных (localhost:5432).
- **Valkey** — in-memory кэш (localhost:6379).
- **Kafka** — брокер сообщений (localhost:9092).
- **Инициализация Kafka-топика** — при старте поднимается `trade-events` (если его ещё нет).

### Сервисы

- `Services/Trading.Api` — HTTP API-заготовка (http://localhost:5194).
- `Services/MarketData.Api` — HTTP API с подключением к PostgreSQL через EF Core (http://localhost:5117).
- `Services/Notification.Worker` — worker-заготовка (http://localhost:5086).

На текущем этапе все сервисы содержат минимальный endpoint `GET /` с ответом `Hello World!`.

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
- `Services/MarketData.Api` — API для рыночных данных (EF Core + PostgreSQL + миграции).
- `Services/Notification.Worker` — сервис фоновой обработки и уведомлений.

## Текущее состояние

Проект находится на стадии каркаса: инфраструктура и сервисы подготовлены, есть контейнеризация сервисов, а в `MarketData.Api` уже создана базовая модель данных и миграции. Бизнес-логика будет добавляться в следующих итерациях.
