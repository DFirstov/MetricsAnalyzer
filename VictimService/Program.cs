using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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


ConcurrentQueue<DateTime> requestTimes = new();

app.MapPost("/chaos/non-linear-latency", async () =>
{
	var now = DateTime.UtcNow;
	requestTimes.Enqueue(now);

	while (requestTimes.TryPeek(out var timestamp) && (now - timestamp).TotalSeconds >= 1)
	{
		requestTimes.TryDequeue(out _);
	}

	var currentRps = requestTimes.Count;
	if (currentRps > 50)
	{
		var extraMs = (int) Math.Pow(currentRps - 50, 1.5);
		await Task.Delay(Random.Shared.Next(extraMs, extraMs + 200));
	}

	return Results.Ok($"Current RPS: {currentRps}");
});


const string dbConnectionString =
	"Host=postgres;" +
	"Database=metrics_test;" +
	"Username=user;" +
	"Password=password;" +
	"Maximum Pool Size=5";

app.MapPost("/chaos/dp-pool-exhaustion", () =>
{
	for (var i = 0; i < 6; i++)
	{
		Task.Run(async () =>
		{
			await using var connection = new NpgsqlConnection(dbConnectionString);
			await connection.OpenAsync();
			await Task.Delay(TimeSpan.FromMinutes(2));
		});
	}

	return Results.Ok("6 DB connections opened (pool limit is 5)");
});


app.MapMetrics();

app.Run();