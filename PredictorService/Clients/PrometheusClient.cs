using System.Globalization;
using System.Text.Json;

namespace PredictorService.Clients;

public static class PrometheusClient
{
	private static readonly HttpClient _client = new() {BaseAddress = new Uri("http://localhost:9090")};

	public static async Task<MetricPoint[]> GetPrometheusData(string query)
	{
		var response = await _client.GetStringAsync($"/api/v1/query?query={Uri.EscapeDataString(query)}");
		return ParsePrometheusResponse(response);
	}

	public static async Task<MetricPoint[]> GetPrometheusHistory(string query, int minutesBack)
	{
		var end = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var start = end - minutesBack * 60;
		const string step = "15s";

		var url = $"/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={start}&end={end}&step={step}";

		var response = await _client.GetStringAsync(url);
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