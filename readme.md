# Metrics Analyzer

Metrics Analyzer — заготовка микросервисного проекта для анализа метрик и предсказания аварийных ситуаций.

Сейчас в репозитории есть локальная инфраструктура и стартовые .NET-сервисы.

## Что уже настроено

### Инфраструктура

- **PostgreSQL** — основное хранилище данных (`localhost:5432`).
- **Valkey** — in-memory кэш (`localhost:6379`).
- **Kafka** — брокер сообщений (`localhost:9092`).
- **Инициализация Kafka-топика** — при старте поднимается `trade-events` (если его ещё нет).

### Сервисы

- `Services/Trading.Api` — HTTP API-заготовка (http://localhost:5194).
- `Services/MarketData.Api` — HTTP API-заготовка (http://localhost:5117).
- `Services/Notification.Worker` — worker-заготовка (http://localhost:5086).

На текущем этапе все сервисы содержат минимальный endpoint `GET /` с ответом `Hello World!`.

## Быстрый старт

### Требования

- Docker
- Docker Compose
- .NET SDK 10.0+

### 1) Запуск инфраструктуры

```bash
docker-compose up -d
```

### 2) Запуск сервисов

Из корня репозитория:

```bash
dotnet run --project Services/Trading.Api
dotnet run --project Services/MarketData.Api
dotnet run --project Services/Notification.Worker
```

## Структура проекта

- `docker-compose.yml` — локальная инфраструктура (PostgreSQL, Valkey, Kafka).
- `MetricsAnalyzer.slnx` — solution-файл.
- `Services/Trading.Api` — API для домена торговых операций.
- `Services/MarketData.Api` — API для рыночных данных.
- `Services/Notification.Worker` — сервис фоновой обработки и уведомлений.

## Текущее состояние

Проект находится на стадии каркаса: инфраструктура и сервисы подготовлены, бизнес-логика будет добавляться в следующих итерациях.
