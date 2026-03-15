namespace PredictorService.Analyzers;

internal static class ZScoreAnalyzer
{
	public static double CalculateZScore(MetricPoint[] data)
	{
		if (data.Length < 20) return 0;

		var values = data
			.Select(d => d.Value)
			.ToArray();

		double current = values.Last();
		double average = values.Average();
		double sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
		double stdDev = Math.Sqrt(sumOfSquares / values.Length);

		if (stdDev < 0.01) stdDev = 0.01;

		double zScore = (current - average) / stdDev;
		return zScore;
	}
}