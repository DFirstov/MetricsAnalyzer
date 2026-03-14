using System.Globalization;
using System.Text.Json;

namespace PredictorService;

public sealed class Worker : BackgroundService
{
	private const int MemoryLimitBytes = 256 * 1024 * 1024;

	private readonly HttpClient _client = new() {BaseAddress = new Uri("http://localhost:9090")};

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await AnalyzeMemoryTrend();

			await Task.Delay(10_000, stoppingToken);
		}
	}

	private async Task AnalyzeMemoryTrend()
	{
		var data = await GetPrometheusData("system_runtime_dotnet_process_memory_working_set[2m]");
		if (data.Length < 5) return;

		int n = data.Length;

		double
			sumX = 0,
			sumY = 0,
			sumXY = 0,
			sumX2 = 0;

		long startTime = data[0].Timestamp;

		foreach (var p in data)
		{
			double x = p.Timestamp - startTime;
			double y = p.Value;

			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
		}

		double denominator = n * sumX2 - sumX * sumX;
		if (Math.Abs(denominator) < 0.0001) return;

		double slope = (n * sumXY - sumX * sumY) / denominator;

		if (slope > 0)
		{
			double remainingMemory = MemoryLimitBytes - data[^1].Value;
			double secondsToOom = remainingMemory / slope;

			if (secondsToOom < 60)
				Console.WriteLine(
					$"[CRITICAL] OOM Prediction: collapse in {secondsToOom:F0}s! Slope: {slope / 1024 / 1024:F2} MB/s");
		}
	}

	private async Task<MetricPoint[]> GetPrometheusData(string query)
	{
		var response = await _client.GetStringAsync($"/api/v1/query?query={Uri.EscapeDataString(query)}");
		return ParsePrometheusResponse(response);
	}

	private static MetricPoint[] ParsePrometheusResponse(string json)
	{
		var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
		var response = JsonSerializer.Deserialize<PrometheusResponse>(json, options);

		var firstResult = response?.Data?.Result?.FirstOrDefault();
		if (firstResult == null) return [];

		var sourceValues =
			firstResult.Values ??
			(firstResult.Value != null
				? [firstResult.Value]
				: []);

		return sourceValues
			.Select(pair =>
			{
				var timestamp = (long) pair[0].GetDouble();
				var value = double.Parse(pair[1].GetString()!, CultureInfo.InvariantCulture);

				return new MetricPoint(timestamp, value);
			})
			.ToArray();
	}
}

public class PrometheusResponse
{
	public DataNode? Data { get; set; }
}

public class DataNode
{
	public ResultNode[]? Result { get; set; }
}

public class ResultNode
{
	public JsonElement[][]? Values { get; set; }
	public JsonElement[]? Value { get; set; }
}

public record MetricPoint(
	long Timestamp,
	double Value);