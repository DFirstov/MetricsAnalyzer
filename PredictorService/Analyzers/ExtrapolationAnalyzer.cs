namespace PredictorService.Analyzers;

internal static class ExtrapolationAnalyzer
{
	public static double CalculateSecondsToLimit(MetricPoint[] data, double limit)
	{
		int n = data.Length;
		if (n < 5) return double.PositiveInfinity;

		double
			sumX = 0,
			sumY = 0,
			sumXy = 0,
			sumX2 = 0;

		long startTime = data[0].Timestamp;

		foreach (var p in data)
		{
			double x = p.Timestamp - startTime;
			double y = p.Value;

			sumX += x;
			sumY += y;
			sumXy += x * y;
			sumX2 += x * x;
		}

		double denominator = n * sumX2 - sumX * sumX;
		if (Math.Abs(denominator) < 0.0001) return double.PositiveInfinity;

		double slope = (n * sumXy - sumX * sumY) / denominator;
		if (slope <= 0) return double.PositiveInfinity;

		double remainder = limit - data[^1].Value;
		if (remainder <= 0) return 0;

		double secondsToLimit = remainder / slope;
		return secondsToLimit;
	}
}