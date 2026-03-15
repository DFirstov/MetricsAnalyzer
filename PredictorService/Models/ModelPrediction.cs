using Microsoft.ML.Data;

namespace PredictorService.Models;

public class ModelPrediction
{
	[VectorType(3)]
	public double[] Score { get; set; }
}