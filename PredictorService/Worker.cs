using System.Globalization;
using System.Text.Json;
using PredictorService.Analyzers;

namespace PredictorService;

public sealed class Worker : BackgroundService
{
	private const int MemoryLimitBytes = 256 * 1024 * 1024;
	private const int CpuLimit = 50;

	private readonly HttpClient _client = new() {BaseAddress = new Uri("http://localhost:9090")};

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await ExtrapolateMemory();
			await ExtrapolateCpu();

			await Task.Delay(10_000, stoppingToken);
		}
	}

	private async Task ExtrapolateMemory()
	{
		var data = await GetPrometheusData("system_runtime_dotnet_process_memory_working_set[2m]");
		double secondsToOom = ExtrapolationAnalyzer.CalculateSecondsToLimit(data, MemoryLimitBytes);

		if (secondsToOom < 60)
		{
			Console.WriteLine($"OOM in {secondsToOom:F0}s");
		}
	}

	private async Task ExtrapolateCpu()
	{
		var data = await GetPrometheusData("system_runtime_cpu_usage[2m]");
		double secondsToThrottling = ExtrapolationAnalyzer.CalculateSecondsToLimit(data, CpuLimit);

		if (secondsToThrottling < 60)
		{
			Console.WriteLine($"CPU throttling in {secondsToThrottling:F0}s");
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