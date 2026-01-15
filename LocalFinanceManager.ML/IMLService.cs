namespace LocalFinanceManager.ML;

/// <summary>
/// Service interface for ML.NET model training and inference operations.
/// Implementation will be added in MVP-5 (Learning Categorization).
/// </summary>
public interface IMLService
{
    /// <summary>
    /// Trains a new ML model on labeled transaction data.
    /// </summary>
    /// <param name="trainingWindowDays">Number of days to look back for training data</param>
    /// <returns>Trained model metrics</returns>
    Task<ModelTrainingResult> TrainModelAsync(int trainingWindowDays);

    /// <summary>
    /// Predicts category for a transaction using the active ML model.
    /// </summary>
    /// <param name="transactionId">Transaction ID to predict category for</param>
    /// <returns>Category prediction with confidence and explanation</returns>
    Task<CategoryPrediction?> PredictCategoryAsync(Guid transactionId);

    /// <summary>
    /// Gets the currently active ML model version and metadata.
    /// </summary>
    Task<ActiveModelInfo?> GetActiveModelAsync();

    /// <summary>
    /// Loads and activates a specific model version.
    /// </summary>
    Task ActivateModelAsync(int modelVersion);
}

/// <summary>
/// Result of model training operation.
/// </summary>
public class ModelTrainingResult
{
    public int Version { get; set; }
    public DateTime TrainedAt { get; set; }
    public int SampleSize { get; set; }
    public int CategoryCount { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public bool MeetsApprovalThreshold { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Category prediction result with explanation.
/// </summary>
public class CategoryPrediction
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<FeatureContribution> TopFeatures { get; set; } = new();
    public int ModelVersion { get; set; }
}

/// <summary>
/// Feature contribution to prediction.
/// </summary>
public class FeatureContribution
{
    public string FeatureName { get; set; } = string.Empty;
    public string FeatureValue { get; set; } = string.Empty;
    public float Importance { get; set; }
}

/// <summary>
/// Active model information.
/// </summary>
public class ActiveModelInfo
{
    public int Version { get; set; }
    public DateTime TrainedAt { get; set; }
    public string Metrics { get; set; } = string.Empty;
}

