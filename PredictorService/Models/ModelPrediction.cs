using Microsoft.ML.Data;

namespace PredictorService.Models;

public class ModelPrediction
{
	[VectorType(3)]
	public required double[] Score { get; set; }
}