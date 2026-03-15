# Metrics Analyzer

## Запуск

1. Поднять инфраструктуру (PostgreSQL, Prometheus, Grafana), а также сервис и имитатор трафика:

   ```shell
   docker-compose up --build -d
   ```

2. Подождать минут 15, чтобы накопились метрики в Prometheus.
3. Запустить из Rider PredictorService.
4. Управлять всем этим можно по адресам:
   - http://localhost:8080/swagger — сам сервис, над которым экспериментируем.
   - http://localhost:9090 — Prometheus
   - http://localhost:3000 — логин `admin`, пароль `admin`