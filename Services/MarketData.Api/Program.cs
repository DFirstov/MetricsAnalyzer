using MarketData.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Берём строку подключения из appsettings.* для текущего окружения.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext, через который приложение работает с PostgreSQL.
builder.Services.AddDbContext<MarketDbContext>(options =>
{
	options.UseNpgsql(connectionString);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	// При старте сервиса автоматически применяем все новые миграции БД.
	// Это упрощает локальный запуск: схема базы создаётся/обновляется без ручных команд.
	var dbContext = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
	dbContext.Database.Migrate();
}

app.MapGet("/", () => "Hello World!");

app.Run();