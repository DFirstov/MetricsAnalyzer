using System.Text.Json;
using MarketData.Api.Data;
using MarketData.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace MarketData.Api.Controllers;

/// <summary>
/// Контроллер для обработки запросов по акциям.
/// </summary>
/// <param name="db">Контекст подключения к БД.</param>
/// <param name="redis">Объект подключения к Redis (Valkey).</param>
[ApiController]
[Route("api/[controller]")]
public sealed class StocksController(MarketDbContext db, IConnectionMultiplexer redis) : ControllerBase
{
	/// <summary>
	/// «Логическое подключение» к Redis. IConnectionMultiplexer создаётся один раз на всё приложение,
	/// а через GetDatabase() мы получаем объект для выполнения команд кэша в текущем запросе.
	/// </summary>
	private readonly IDatabase _cache = redis.GetDatabase();

	/// <summary>
	/// Получает текущую информацию об акции по её тикеру.
	/// </summary>
	/// <param name="ticker">Тикер акции.</param>
	/// <returns>Текущая информация об акции.</returns>
	[HttpGet("{ticker}")]
	public async Task<IActionResult> Get(string ticker)
	{
		// Формируем ключ кэша в едином формате, чтобы «aapl» и «AAPL» считались одним тикером.
		string cacheKey = $"price:{ticker.ToUpper()}";

		// Сначала пытаемся отдать данные из Redis — это быстрее, чем запрос в БД.
		var cachedPrice = await _cache.StringGetAsync(cacheKey);
		if (cachedPrice.HasValue)
		{
			// В кэше хранится JSON, поэтому десериализуем его обратно в модель Stock.
			return Ok(JsonSerializer.Deserialize<Stock>((string) cachedPrice!));
		}

		// Если в кэше ничего нет, ищем запись в PostgreSQL.
		var stock = await db.Stocks.FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.ToUpper());
		if (stock == null)
		{
			// Явно сообщаем клиенту, что такой тикер не найден.
			return NotFound(new {Message = "Ticker not found"});
		}

		// Кладём найденную цену в кэш на 1 минуту, чтобы разгрузить базу при частых запросах.
		await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(stock), TimeSpan.FromMinutes(1));
		return Ok(stock);
	}

	/// <summary>
	/// Добавление некоторых данных для примера (сидирование).
	/// </summary>
	/// <returns>200 OK — если сидирование успешно; 400 Bad Request — если сидирование уже было сделано.</returns>
	[HttpPost("seed")]
	public async Task<IActionResult> Seed()
	{
		// Чтобы избежать дублей, запрещаем повторное сидирование,
		// если в таблице уже есть хотя бы одна запись.
		if (await db.Stocks.AnyAsync())
		{
			return BadRequest("Already seeded");
		}

		// Добавляем стартовые данные для локальной разработки и быстрой проверки API.
		db.Stocks.AddRange(
			new Stock {Ticker = "AAPL", Price = 150.25m, LastUpdated = DateTime.UtcNow},
			new Stock {Ticker = "MSFT", Price = 300.50m, LastUpdated = DateTime.UtcNow},
			new Stock {Ticker = "GOOGL", Price = 2800.10m, LastUpdated = DateTime.UtcNow}
		);

		await db.SaveChangesAsync();

		// Возвращаем простой статус, что данные успешно добавлены.
		return Ok("Seeded");
	}
}