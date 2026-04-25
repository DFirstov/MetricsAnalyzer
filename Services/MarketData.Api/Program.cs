using MarketData.Api.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Берём строку подключения из appsettings.* для текущего окружения.
var postgresConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext, через который приложение работает с PostgreSQL.
builder.Services.AddDbContext<MarketDbContext>(options =>
{
	options.UseNpgsql(postgresConnectionString);
});

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	// При старте сервиса автоматически применяем все новые миграции БД.
	// Это упрощает локальный запуск: схема базы создаётся/обновляется без ручных команд.
	var dbContext = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
	dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();