using PredictorService.Clients;

namespace PredictorService.Models;

public class ModelInput
{
	public float Rps { get; set; }
	public float ErrorRate { get; set; }
	public float Latency { get; set; }
	public float Cpu { get; set; }
	public float Memory { get; set; }

	public static ModelInput[] FromMetrics(
		IEnumerable<MetricPoint> rps,
		IEnumerable<MetricPoint> errorRate,
		IEnumerable<MetricPoint> latency,
		IEnumerable<MetricPoint> cpu,
		IEnumerable<MetricPoint> memory)
	{
		var joined =
			from r in rps
			join e in errorRate on Math.Round(r.Timestamp / 5.0) equals Math.Round(e.Timestamp / 5.0)
			join l in latency on Math.Round(r.Timestamp / 5.0) equals Math.Round(l.Timestamp / 5.0)
			join c in cpu on Math.Round(r.Timestamp / 5.0) equals Math.Round(c.Timestamp / 5.0)
			join m in memory on Math.Round(r.Timestamp / 5.0) equals Math.Round(m.Timestamp / 5.0)
			select new ModelInput
			{
				Rps = (float) r.Value,
				ErrorRate = (float) e.Value,
				Latency = (float) l.Value,
				Cpu = (float) c.Value,
				Memory = (float) m.Value
			};

		return joined.ToArray();
	}
}