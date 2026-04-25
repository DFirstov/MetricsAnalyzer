using System.Diagnostics.CodeAnalysis;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Общий HTTP-клиент используем на весь запуск генератора,
// чтобы не создавать лишние сокеты на каждую операцию.
using HttpClient httpClient = new();

// Подготавливаем тестовые данные перед запуском нагрузки.
await httpClient.PostAsync("http://localhost:5117/api/stocks/seed", null);

// Создаём сценарий нагрузочного тестирования
var scenario = Scenario
	.Create("Invest App Load", async context =>
	{
		// Пока единственный шаг — запрашиваем информацию по акциям Apple.
		var step = await Step.Run(
			"Fetch Stock Price",
			context,
			[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
			async () =>
			{
				var request = Http
					.CreateRequest("GET", "http://localhost:5117/api/stocks/AAPL")
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