using System.Diagnostics.CodeAnalysis;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Общий HTTP-клиент используем на весь запуск генератора,
// чтобы не создавать лишние сокеты на каждую операцию.
using HttpClient httpClient = new();

// Подготавливаем тестовые данные перед запуском нагрузки.
// Здесь генерируем большой набор синтетических тикеров, чтобы запросы распределялись по «холодным» и «горячим» ключам кэша.
const int tickersCount = 1_000_000;
await httpClient.PostAsync($"http://localhost:5117/api/stocks/seed/{tickersCount}", null);

// Создаём сценарий нагрузочного тестирования
var scenario = Scenario
	.Create("Invest App Load", async context =>
	{
		// Выполняем запрос по случайному тикеру из заранее сгенерированного диапазона.
		var step = await Step.Run(
			"Fetch Stock Price",
			context,
			[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
			async () =>
			{
				var ticker = GetRandomTicker(tickersCount);

				var request = Http
					.CreateRequest("GET", $"http://localhost:5117/api/stocks/{ticker}")
					.WithHeader("Accept", "application/json");

				return await Http.Send(httpClient, request);
			});

		return step;
	})
	.WithWarmUpDuration(TimeSpan.FromSeconds(5)) // Прогрев 5 секунд,
	.WithLoadSimulations(                        // потом основная симуляция.
		Simulation.Inject(
			rate: 50,                          // Генерируем 50 запросов
			interval: TimeSpan.FromSeconds(1), // в секунду
			during: TimeSpan.FromMinutes(5))); // в течение 5 минут.

NBomberRunner
	.RegisterScenarios(scenario)
	.Run();

/// <summary>
/// Возвращает случайный тикер в формате <c>TKR0000000</c> из диапазона с 1 до <paramref name="totalCount"/>.
/// </summary>
/// <param name="totalCount">Общее количество доступных сгенерированных тикеров.</param>
/// <returns>Строка тикера для запроса к API.</returns>
static string GetRandomTicker(int totalCount)
{
	// Смещаем распределение в сторону меньших индексов (Math.Pow(..., 4)),
	// чтобы чаще попадать в одни и те же тикеры и наблюдать эффект кэширования.
	double random = Random.Shared.NextDouble();
	double biasedRandom = Math.Pow(random, 4);

	return $"TKR{biasedRandom * totalCount:0000000}";
}