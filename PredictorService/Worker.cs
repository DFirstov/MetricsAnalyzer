using PredictorService.Analyzers;
using PredictorService.Clients;

namespace PredictorService;

public sealed class Worker : BackgroundService
{
	private const int MemoryLimitBytes = 256 * 1024 * 1024;
	private const int CpuLimit = 50;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await ExtrapolateMemory();
			await ExtrapolateCpu();

			await ZScoreMemory();
			await ZScoreCpu();

			await Task.Delay(10_000, stoppingToken);
		}
	}

	private static async Task ExtrapolateMemory()
	{
		var data = await PrometheusClient.GetPrometheusData("system_runtime_dotnet_process_memory_working_set[2m]");
		double secondsToOom = ExtrapolationAnalyzer.CalculateSecondsToLimit(data, MemoryLimitBytes);

		if (secondsToOom < 60)
		{
			Console.WriteLine($"Extrapolate: OOM in {secondsToOom:F0}s");
		}
	}

	private static async Task ExtrapolateCpu()
	{
		var data = await PrometheusClient.GetPrometheusData("system_runtime_cpu_usage[2m]");
		double secondsToThrottling = ExtrapolationAnalyzer.CalculateSecondsToLimit(data, CpuLimit);

		if (secondsToThrottling < 60)
		{
			Console.WriteLine($"Extrapolate: CPU throttling in {secondsToThrottling:F0}s");
		}
	}

	private static async Task ZScoreMemory()
	{
		var data = await PrometheusClient.GetPrometheusData("system_runtime_dotnet_process_memory_working_set[5m]");
		double zScore = ZScoreAnalyzer.CalculateZScore(data);

		if (zScore > 3)
		{
			Console.WriteLine($"ZScore: memory consumption has spiked");
		}
	}

	private static async Task ZScoreCpu()
	{
		var data = await PrometheusClient.GetPrometheusData("system_runtime_cpu_usage[5m]");
		double zScore = ZScoreAnalyzer.CalculateZScore(data);

		if (zScore > 3)
		{
			Console.WriteLine($"ZScore: CPU consumption has spiked");
		}
	}
}