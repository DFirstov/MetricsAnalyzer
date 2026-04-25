using MarketData.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Api.Data;

/// <inheritdoc />
/// <summary>
/// Контекст БД: описывает, с какими таблицами работаем и какие правила на них действуют.
/// </summary>
/// <param name="options">Настройки подключения к БД.</param>
public class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
	/// <summary>
	/// Таблица акций в базе данных.
	/// </summary>
	public DbSet<Stock> Stocks => Set<Stock>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder
			.Entity<Stock>()
			.HasIndex(stock => stock.Ticker) // Для тикера делаем уникальный индекс, чтобы не было дублей одной и той же акции.
			.IsUnique();

		base.OnModelCreating(modelBuilder);
	}
}