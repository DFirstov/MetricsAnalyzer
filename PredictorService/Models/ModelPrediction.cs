using Microsoft.ML.Data;

namespace PredictorService.Models;

public sealed class ModelPrediction
{
	[ColumnName("Score")]
	public float Score { get; set; }

	[ColumnName("PredictedLabel")]
	public bool IsAnomaly { get; set; }
}