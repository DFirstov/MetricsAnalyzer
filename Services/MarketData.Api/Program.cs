using MarketData.Api.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

// Создаём объект для сборки приложения.
var builder = WebApplication.CreateBuilder(args);

// Берём строку подключения из appsettings.* для текущего окружения.
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext, через который приложение работает с PostgreSQL.
builder.Services.AddDbContext<MarketDbContext>(options => options.UseNpgsql(postgresConnectionString));

// Берём строку подключения к Redis (Valkey) и добавляем менеджер подключений.
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// Добавляем контроллеры и Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Собираем приложение.
var app = builder.Build();

// При старте сервиса автоматически применяем все новые миграции БД.
// Это упрощает локальный запуск: схема базы создаётся/обновляется без ручных команд.
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
	dbContext.Database.Migrate();
}

// Включаем Swagger.
app.UseSwagger();
app.UseSwaggerUI();

// Включаем контроллеры.
app.MapControllers();

// Запускаем приложение.
app.Run();