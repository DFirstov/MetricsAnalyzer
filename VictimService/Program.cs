using System.Collections.Concurrent;
using Npgsql;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpMetrics();


app.MapGet("/", () => "Service is healthy");


List<byte[]> memoryLeakContainer = [];

app.MapPost("/chaos/memory-leak", () =>
{
	var chunk = new byte[30 * 1024 * 1024];
	Array.Fill(chunk, (byte) 1);
	memoryLeakContainer.Add(chunk);
	return Results.Ok($"Allocated 30MB. Total: {memoryLeakContainer.Count * 30}MB");
});


app.MapPost("/chaos/cpu-spike", (int durationSeconds = 30) =>
{
	for (var i = 0; i < Environment.ProcessorCount; i++)
	{
		Task.Run(() =>
		{
			var end = DateTime.UtcNow.AddSeconds(durationSeconds);
			while (DateTime.UtcNow < end)
			{
			}
		});
	}

	return Results.Ok($"CPU load started on {Environment.ProcessorCount} cores");
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