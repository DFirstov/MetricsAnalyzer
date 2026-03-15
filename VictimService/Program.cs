using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpMetrics();


app.MapGet("/", () => "Service is healthy");


List<byte[]> memoryLeakContainer = [];
bool isMemoryLeak;

app.MapPost("/chaos/memory-leak/start", ([FromServices] ILogger<Program> logger) =>
{
	isMemoryLeak = true;
	Task.Run(async () =>
	{
		while (isMemoryLeak)
		{
			bool clear = Random.Shared.NextDouble() < 0.2;
			if (clear)
			{
				var indexOfBufferToRemove = Random.Shared.Next(0, memoryLeakContainer.Count);
				memoryLeakContainer.RemoveAt(indexOfBufferToRemove);
				GC.Collect();
				var allocated = memoryLeakContainer.Sum(buffer => buffer.Length) / 1024 / 1024;
				logger.LogInformation("Memory usage decreased. Allocated megabytes: {Allocated}", allocated);
			}
			else
			{
				var megabytes = 5 * Random.Shared.NextDouble();
				var bytes = (int) (megabytes * 1024 * 1024);
				var buffer = new byte[bytes];
				Array.Fill(buffer, (byte) 1);
				memoryLeakContainer.Add(buffer);
				var allocated = memoryLeakContainer.Sum(b => b.Length) / 1024 / 1024;
				logger.LogWarning("Memory usage increased. Allocated megabytes: {Allocated}", allocated);
			}

			await Task.Delay(TimeSpan.FromSeconds(1));
		}

		memoryLeakContainer.Clear();
		GC.Collect();
		logger.LogInformation("Memory leak stopped.");
	});
});

app.MapPost("/chaos/memory-leak/stop", void () => isMemoryLeak = false);


app.MapPost("/chaos/cpu/linear", (int durationSeconds = 120) =>
{
	Task.Run(async () =>
	{
		var duration = TimeSpan.FromSeconds(durationSeconds);

		using CancellationTokenSource cancellationTokenSource = new(duration);
		var stopwatch = Stopwatch.StartNew();

		while (!cancellationTokenSource.Token.IsCancellationRequested)
		{
			double progress = stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds;
			double intensity = Math.Min(progress, 1.0);

			var innerStopwatch = Stopwatch.StartNew();
			while (innerStopwatch.ElapsedMilliseconds < intensity * 100)
			{
				_ = Math.Sqrt(Random.Shared.NextDouble());
			}

			await Task.Delay((int) ((1.0 - intensity) * 100));
		}
	});
});

app.MapPost("/chaos/cpu/spike", (int durationSeconds = 120) =>
{
	Task.Run(() =>
	{
		var duration = TimeSpan.FromSeconds(durationSeconds);
		using CancellationTokenSource cancellationTokenSource = new(duration);

		while (!cancellationTokenSource.Token.IsCancellationRequested)
		{
			_ = Math.Sqrt(Random.Shared.NextDouble());
		}
	});
});

app.MapPost("/chaos/cpu/oscillation", (int durationSeconds = 120) =>
{
	Task.Run(async () =>
	{
		var duration = TimeSpan.FromSeconds(durationSeconds);
		using CancellationTokenSource cancellationTokenSource = new(duration);

		bool spike = true;

		while (!cancellationTokenSource.Token.IsCancellationRequested)
		{
			var periodEnd = DateTime.UtcNow.AddSeconds(5 * Random.Shared.NextDouble());

			while (periodEnd > DateTime.UtcNow && !cancellationTokenSource.Token.IsCancellationRequested)
			{
				if (spike)
				{
					_ = Math.Sqrt(Random.Shared.NextDouble());
				}
				else
				{
					await Task.Delay(50);
				}
			}

			spike = !spike;
		}
	});
});


const double errorChance = 0.005;

app.MapGet("/api/data", async () =>
{
	// В небольшом проценте случаев имитируем ошибку
	if (Random.Shared.NextDouble() < errorChance)
	{
		return Results.Problem("Random internal error", statusCode: 500);
	}

	// Имитируем какие-то расчёты
	double result = Enumerable
		.Range(0, 1_000_000)
		.Sum(_ => Math.Sqrt(Random.Shared.NextDouble()));

	// Возвращаем успешный результат
	return Results.Ok(new
	{
		Value = result,
		Status = "Success"
	});
});


app.MapMetrics();

app.Run();