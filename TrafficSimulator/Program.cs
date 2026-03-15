using System.Collections.Concurrent;
using System.Diagnostics;

HttpClient httpClient = new() {BaseAddress = new Uri("http://localhost:8080")};
double baseRps = 50;
double amplitude = 40;
int tick = 0;

var outerStopwatch = Stopwatch.StartNew();

while (true)
{
	var stopwatch = Stopwatch.StartNew();

	double timeFactor = tick / 60.0;
	int targetRps = (int) (baseRps + Math.Sin(timeFactor) * amplitude + Random.Shared.Next(-5, 6));
	targetRps = Math.Max(1, targetRps);

	var metrics = new ConcurrentBag<(bool Success, long Latency)>();

	var tasks = Enumerable
		.Range(0, targetRps)
		.Select(async _ =>
		{
			var innerStopwatch = Stopwatch.StartNew();

			try
			{
				var response = await httpClient.GetAsync("/api/data");
				metrics.Add((response.IsSuccessStatusCode, innerStopwatch.ElapsedMilliseconds));
			}
			catch
			{
				metrics.Add((false, innerStopwatch.ElapsedMilliseconds));
			}
		});

	await Task.WhenAll(tasks);

	int errorCount = metrics.Count(m => !m.Success);
	double averageLatency = metrics.Any()
		? metrics.Average(m => m.Latency)
		: 0;

	Console.WriteLine(
		$"{outerStopwatch.Elapsed} | " +
		$"RPS: {targetRps:D3} | " +
		$"Latency: {averageLatency:F0}ms | " +
		$"Errors: {errorCount:D2}");

	tick++;
	var elapsed = stopwatch.ElapsedMilliseconds;
	if (elapsed < 1000) await Task.Delay(1000 - (int) elapsed);
}