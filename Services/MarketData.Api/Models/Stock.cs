using System.Diagnostics.CodeAnalysis;

namespace MarketData.Api.Models;

/// <summary>
/// Модель одной записи по акции.
/// </summary>
[SuppressMessage("ReSharper", "EntityFramework.ModelValidation.UnlimitedStringLength")]
public sealed class Stock
{
	/// <summary>
	/// Внутренний идентификатор записи в таблице.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Биржевой тикер (например, AAPL). Должен быть уникальным.
	/// </summary>
	public string Ticker { get; set; } = null!;

	/// <summary>
	/// Текущая цена акции.
	/// </summary>
	public decimal Price { get; set; }

	/// <summary>
	/// Время последнего обновления цены (UTC).
	/// </summary>
	public DateTime LastUpdated { get; set; }
}