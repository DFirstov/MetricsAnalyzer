using Microsoft.ML;
using PredictorService.Clients;
using PredictorService.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PredictorService.Training;

internal static class AnomalyDetectionTraining
{
	private const bool ReuseLastTrainingData = true;
	
	public static async Task<PredictionEngine<ModelInput, ModelPrediction>> CreatePredictionEngine()
	{
		var data = await FetchTrainingData();
		return TrainModel(data);
	}

	public static async Task<ModelInput> GetCurrentPointFromPrometheus()
	{
		var rpsTask = PrometheusClient.GetPrometheusData("rate(http_requests_received_total[1m])");
		var errTask = PrometheusClient.GetPrometheusData("rate(http_requests_received_total{code=~\"5..\"}[1m]) / rate(http_requests_received_total[1m])");
		var latTask = PrometheusClient.GetPrometheusData("rate(http_request_duration_seconds_sum[1m]) / rate(http_requests_received_total[1m])");
		var cpuTask = PrometheusClient.GetPrometheusData("rate(process_cpu_seconds_total[1m])");
		var memTask = PrometheusClient.GetPrometheusData("system_runtime_dotnet_process_memory_working_set");

		await Task.WhenAll(rpsTask, latTask, cpuTask, memTask, errTask);

		var input = new ModelInput
		{
			Rps = (float) (rpsTask.Result.LastOrDefault()?.Value ?? 0),
			Latency = (float) (latTask.Result.LastOrDefault()?.Value ?? 0),
			Cpu = (float) (cpuTask.Result.LastOrDefault()?.Value ?? 0),
			Memory = (float) (memTask.Result.LastOrDefault()?.Value ?? 0),
			ErrorRate = (float) (errTask.Result.LastOrDefault()?.Value ?? 0)
		};

		if (float.IsNaN(input.Latency)) input.Latency = 0;
		if (float.IsNaN(input.ErrorRate)) input.ErrorRate = 0;

		return input;
	}

	private static async Task<ModelInput[]> FetchTrainingData()
	{
		const string? trainingDataDirectory = "training-data";
		
		if (ReuseLastTrainingData && Directory.Exists(trainingDataDirectory))
		{
			var oldTrainingData = Directory
				.EnumerateFiles(trainingDataDirectory)
				.Order()
				.LastOrDefault();

			if (oldTrainingData != null)
			{
				await using var inputFile = File.OpenRead(oldTrainingData);
				var result = await JsonSerializer.DeserializeAsync<ModelInput[]>(inputFile);
				if (result != null)
				{
					return result;
				}
			}
		}
		
		var rpsTask = PrometheusClient.GetPrometheusHistory("rate(http_requests_received_total[1m])", 15);
		var errTask = PrometheusClient.GetPrometheusHistory("rate(http_requests_received_total{code=~\"5..\"}[1m]) / rate(http_requests_received_total[1m])", 15);
		var latTask = PrometheusClient.GetPrometheusHistory("rate(http_request_duration_seconds_sum[1m]) / rate(http_requests_received_total[1m])", 15);
		var cpuTask = PrometheusClient.GetPrometheusHistory("rate(process_cpu_seconds_total[1m])", 15);
		var memTask = PrometheusClient.GetPrometheusHistory("system_runtime_dotnet_process_memory_working_set", 15);

		await Task.WhenAll(rpsTask, errTask, latTask, cpuTask, memTask);

		var trainingData = ModelInput.FromMetrics(
			rpsTask.Result,
			errTask.Result,
			latTask.Result,
			cpuTask.Result,
			memTask.Result);

		foreach (var item in trainingData)
		{
			if (float.IsNaN(item.Latency)) item.Latency = 0;
			if (float.IsNaN(item.ErrorRate)) item.ErrorRate = 0;
		}

		Directory.CreateDirectory(trainingDataDirectory);
		await using var outputFile = File.OpenWrite($"{trainingDataDirectory}/training-data-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.json");
		await JsonSerializer.SerializeAsync(outputFile, trainingData);

		return trainingData;
	}

	private static PredictionEngine<ModelInput, ModelPrediction> TrainModel(ModelInput[] data)
	{
		MLContext mlContext = new();

		var trainingDataView = mlContext.Data.LoadFromEnumerable(data);

		var pipeline = mlContext.Transforms
			.ReplaceMissingValues(
			[
				new InputOutputColumnPair(nameof(ModelInput.Rps)),
				new InputOutputColumnPair(nameof(ModelInput.ErrorRate)),
				new InputOutputColumnPair(nameof(ModelInput.Latency)),
				new InputOutputColumnPair(nameof(ModelInput.Cpu)),
				new InputOutputColumnPair(nameof(ModelInput.Memory))
			])
			.Append(mlContext.Transforms.Concatenate(
				"Features",
				nameof(ModelInput.Rps),
				nameof(ModelInput.ErrorRate),
				nameof(ModelInput.Latency),
				nameof(ModelInput.Cpu),
				nameof(ModelInput.Memory)))
			.Append(mlContext.Transforms.NormalizeMinMax("Features"))
			.Append(mlContext.AnomalyDetection.Trainers.RandomizedPca(
				featureColumnName: "Features",
				rank: 3));

		var transformer = pipeline.Fit(trainingDataView);
		var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelPrediction>(transformer);
		return predictionEngine;
	}
}